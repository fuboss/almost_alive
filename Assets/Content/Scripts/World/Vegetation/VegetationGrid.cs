using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.World.Vegetation {
  /// <summary>
  /// Runtime vegetation state grid. Syncs with Unity Terrain DetailLayers.
  /// Optimized: tracks burning cells in HashSet, region-based sync.
  /// </summary>
  public class VegetationGrid {
    private readonly Terrain _terrain;
    private readonly TerrainData _terrainData;
    private readonly int _resolution;
    private readonly int _layerCount;
    private readonly VegetationCell[,] _cells;
    
    // Dirty tracking for batched updates
    private readonly HashSet<Vector2Int> _dirtyCells = new();
    private readonly HashSet<int> _dirtyLayers = new();
    
    // Track burning cells separately (avoid full grid scan)
    private readonly HashSet<Vector2Int> _burningCells = new();
    
    // Cached terrain info
    private readonly Vector3 _terrainPos;
    private readonly Vector3 _terrainSize;
    private readonly float _cellWorldSize;

    public int Resolution => _resolution;
    public int LayerCount => _layerCount;
    public bool HasDirtyCells => _dirtyCells.Count > 0;
    public int BurningCellCount => _burningCells.Count;

    public VegetationGrid(Terrain terrain) {
      _terrain = terrain;
      _terrainData = terrain.terrainData;
      _resolution = _terrainData.detailResolution;
      _layerCount = _terrainData.detailPrototypes.Length;
      _terrainPos = terrain.transform.position;
      _terrainSize = _terrainData.size;
      _cellWorldSize = _terrainSize.x / _resolution;
      
      // Initialize cells
      _cells = new VegetationCell[_resolution, _resolution];
      
      // Load current state from terrain
      LoadFromTerrain();
      
      Debug.Log($"[VegetationGrid] Initialized {_resolution}x{_resolution} grid with {_layerCount} layers");
    }

    #region State Loading

    /// <summary>
    /// Load vegetation state from terrain detail layers.
    /// </summary>
    public void LoadFromTerrain() {
      _burningCells.Clear();
      
      for (var layer = 0; layer < _layerCount; layer++) {
        var detailLayer = _terrainData.GetDetailLayer(0, 0, _resolution, _resolution, layer);
        
        for (var z = 0; z < _resolution; z++) {
          for (var x = 0; x < _resolution; x++) {
            // Initialize cell if needed
            if (_cells[x, z].densities == null) {
              _cells[x, z] = VegetationCell.Create(_layerCount);
            }
            
            _cells[x, z].densities[layer] = (byte)detailLayer[z, x];
          }
        }
      }
      
      _dirtyCells.Clear();
      _dirtyLayers.Clear();
    }

    #endregion

    #region Coordinate Conversion

    /// <summary>
    /// Convert world position to detail grid coordinates.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos) {
      var normalizedX = (worldPos.x - _terrainPos.x) / _terrainSize.x;
      var normalizedZ = (worldPos.z - _terrainPos.z) / _terrainSize.z;
      
      var x = Mathf.Clamp(Mathf.FloorToInt(normalizedX * _resolution), 0, _resolution - 1);
      var z = Mathf.Clamp(Mathf.FloorToInt(normalizedZ * _resolution), 0, _resolution - 1);
      
      return new Vector2Int(x, z);
    }

    /// <summary>
    /// Convert grid coordinates to world position (cell center).
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos) {
      var normalizedX = (gridPos.x + 0.5f) / _resolution;
      var normalizedZ = (gridPos.y + 0.5f) / _resolution;
      
      var worldX = _terrainPos.x + normalizedX * _terrainSize.x;
      var worldZ = _terrainPos.z + normalizedZ * _terrainSize.z;
      
      return new Vector3(
        worldX,
        _terrain.SampleHeight(new Vector3(worldX, 0, worldZ)),
        worldZ
      );
    }

    /// <summary>
    /// Check if grid coordinates are valid.
    /// </summary>
    public bool IsValidCoord(Vector2Int coord) {
      return coord.x >= 0 && coord.x < _resolution && coord.y >= 0 && coord.y < _resolution;
    }

    #endregion

    #region Cell Access

    /// <summary>
    /// Get cell at grid coordinates.
    /// </summary>
    public ref VegetationCell GetCell(Vector2Int coord) {
      return ref _cells[coord.x, coord.y];
    }

    /// <summary>
    /// Get cell at world position.
    /// </summary>
    public ref VegetationCell GetCellAt(Vector3 worldPos) {
      var coord = WorldToGrid(worldPos);
      return ref _cells[coord.x, coord.y];
    }

    #endregion

    #region Modification API

    /// <summary>
    /// Clear vegetation at world position.
    /// </summary>
    public void ClearAt(Vector3 worldPos) {
      var coord = WorldToGrid(worldPos);
      ref var cell = ref _cells[coord.x, coord.y];
      cell.Clear();
      _burningCells.Remove(coord);
      MarkDirty(coord);
    }

    /// <summary>
    /// Clear vegetation in radius around world position.
    /// </summary>
    public void ClearInRadius(Vector3 worldPos, float radius) {
      var center = WorldToGrid(worldPos);
      var gridRadius = Mathf.CeilToInt(radius / _cellWorldSize);
      var radiusSqr = radius * radius;
      
      for (var dx = -gridRadius; dx <= gridRadius; dx++) {
        for (var dz = -gridRadius; dz <= gridRadius; dz++) {
          var coord = new Vector2Int(center.x + dx, center.y + dz);
          if (!IsValidCoord(coord)) continue;
          
          // Check actual world distance
          var cellWorld = GridToWorld(coord);
          var distSqr = (worldPos.x - cellWorld.x) * (worldPos.x - cellWorld.x) + 
                        (worldPos.z - cellWorld.z) * (worldPos.z - cellWorld.z);
          if (distSqr > radiusSqr) continue;
          
          ref var cell = ref _cells[coord.x, coord.y];
          cell.Clear();
          _burningCells.Remove(coord);
          MarkDirty(coord);
        }
      }
    }

    /// <summary>
    /// Set fire at world position.
    /// </summary>
    public void IgniteAt(Vector3 worldPos) {
      var coord = WorldToGrid(worldPos);
      ref var cell = ref _cells[coord.x, coord.y];
      
      if (!cell.HasVegetation) return;
      if (cell.isOnFire) return;
      
      cell.isOnFire = true;
      _burningCells.Add(coord);
      MarkDirty(coord);
    }

    /// <summary>
    /// Process fire spread for one tick. Returns number of active fires.
    /// Optimized: only iterates burning cells, not entire grid.
    /// </summary>
    /// <param name="burnRate">Damage per tick (0-1)</param>
    /// <param name="spreadChance">Chance to spread per attempt</param>
    /// <param name="spreadIterations">Number of spread attempts per cell (1=normal, higher=faster)</param>
    public int ProcessFireTick(float burnRate, float spreadChance, int spreadIterations = 1) {
      if (_burningCells.Count == 0) return 0;
      
      // Copy to avoid modification during iteration
      var currentBurning = new List<Vector2Int>(_burningCells);
      var newFires = new List<Vector2Int>();
      
      foreach (var coord in currentBurning) {
        ref var cell = ref _cells[coord.x, coord.y];
        
        if (!cell.isOnFire) {
          _burningCells.Remove(coord);
          continue;
        }
        
        // Apply burn damage
        cell.ApplyBurn(burnRate);
        MarkDirty(coord);
        
        // Check if burned out
        if (!cell.isOnFire || !cell.HasVegetation) {
          _burningCells.Remove(coord);
          continue;
        }
        
        // Try to spread to neighbors (multiple attempts for faster spread)
        for (var i = 0; i < spreadIterations; i++) {
          if (Random.value < spreadChance) {
            TrySpreadFire(coord, newFires);
          }
        }
      }
      
      // Add new fires
      foreach (var coord in newFires) {
        _burningCells.Add(coord);
      }
      
      return _burningCells.Count;
    }

    private static readonly Vector2Int[] Directions = {
      new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
    };

    private void TrySpreadFire(Vector2Int from, List<Vector2Int> newFires) {
      var dir = Directions[Random.Range(0, 4)];  
      var to = from + dir;
      
      if (!IsValidCoord(to)) return;
      
      ref var cell = ref _cells[to.x, to.y];
      if (!cell.HasVegetation) return;
      if (cell.isOnFire) return;
      
      // Chance to ignite based on vegetation density (more grass = easier to spread)
      // TotalDensity can be 0-32 per layer, with multiple layers
      // Normalize to 0-1 range, minimum 10% chance if any vegetation exists
      var densityFactor = Mathf.Clamp01(cell.TotalDensity / 30f);
      var igniteChance = 0.1f + densityFactor * 0.9f; // 10% to 100%
      
      if (Random.value > igniteChance) return;
      
      cell.isOnFire = true;
      newFires.Add(to);
      MarkDirty(to);
    }

    /// <summary>
    /// Set density for specific layer at position.
    /// </summary>
    public void SetDensity(Vector3 worldPos, int layer, byte density) {
      var coord = WorldToGrid(worldPos);
      ref var cell = ref _cells[coord.x, coord.y];
      cell.SetDensity(layer, density);
      if (cell.isDirty) {
        MarkDirty(coord, layer);
      }
    }

    #endregion

    #region Dirty Tracking & Sync

    private void MarkDirty(Vector2Int coord) {
      _dirtyCells.Add(coord);
      for (var i = 0; i < _layerCount; i++) {
        _dirtyLayers.Add(i);
      }
    }

    private void MarkDirty(Vector2Int coord, int layer) {
      _dirtyCells.Add(coord);
      _dirtyLayers.Add(layer);
    }

    /// <summary>
    /// Sync dirty cells to terrain using region-based approach.
    /// Much faster than rebuilding entire layers.
    /// </summary>
    public void SyncToTerrain() {
      if (_dirtyCells.Count == 0) return;
      
      // Find bounding box of dirty cells
      var minX = int.MaxValue;
      var minZ = int.MaxValue;
      var maxX = int.MinValue;
      var maxZ = int.MinValue;
      
      foreach (var coord in _dirtyCells) {
        if (coord.x < minX) minX = coord.x;
        if (coord.x > maxX) maxX = coord.x;
        if (coord.y < minZ) minZ = coord.y;
        if (coord.y > maxZ) maxZ = coord.y;
      }
      
      // Clamp to valid range
      minX = Mathf.Max(0, minX);
      minZ = Mathf.Max(0, minZ);
      maxX = Mathf.Min(_resolution - 1, maxX);
      maxZ = Mathf.Min(_resolution - 1, maxZ);
      
      var width = maxX - minX + 1;
      var height = maxZ - minZ + 1;
      
      // Sync each dirty layer (region only)
      foreach (var layer in _dirtyLayers) {
        var detailLayer = new int[height, width];
        
        for (var z = 0; z < height; z++) {
          for (var x = 0; x < width; x++) {
            var gx = minX + x;
            var gz = minZ + z;
            detailLayer[z, x] = _cells[gx, gz].densities[layer];
          }
        }
        
        _terrainData.SetDetailLayer(minX, minZ, layer, detailLayer);
      }
      
      // Clear dirty flags
      foreach (var coord in _dirtyCells) {
        _cells[coord.x, coord.y].isDirty = false;
      }
      
      _dirtyCells.Clear();
      _dirtyLayers.Clear();
    }

    #endregion

    #region Queries

    /// <summary>
    /// Get total vegetation density at world position.
    /// </summary>
    public int GetTotalDensityAt(Vector3 worldPos) {
      var coord = WorldToGrid(worldPos);
      return _cells[coord.x, coord.y].TotalDensity;
    }

    /// <summary>
    /// Check if there's vegetation at world position.
    /// </summary>
    public bool HasVegetationAt(Vector3 worldPos) {
      var coord = WorldToGrid(worldPos);
      return _cells[coord.x, coord.y].HasVegetation;
    }

    /// <summary>
    /// Check if cell is on fire at world position.
    /// </summary>
    public bool IsOnFireAt(Vector3 worldPos) {
      var coord = WorldToGrid(worldPos);
      return _cells[coord.x, coord.y].isOnFire;
    }

    /// <summary>
    /// Get all cells currently on fire. O(1) - returns tracked set.
    /// </summary>
    public IReadOnlyCollection<Vector2Int> GetBurningCells() {
      return _burningCells;
    }

    /// <summary>
    /// Extinguish fire at coord.
    /// </summary>
    public void ExtinguishAt(Vector2Int coord) {
      if (!IsValidCoord(coord)) return;
      ref var cell = ref _cells[coord.x, coord.y];
      cell.isOnFire = false;
      _burningCells.Remove(coord);
    }

    /// <summary>
    /// Extinguish all fires.
    /// </summary>
    public void ExtinguishAll() {
      foreach (var coord in _burningCells) {
        _cells[coord.x, coord.y].isOnFire = false;
      }
      _burningCells.Clear();
    }

    #endregion
  }
}
