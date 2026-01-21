using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Vegetation {
  /// <summary>
  /// Manages vegetation grid and provides global access for runtime vegetation manipulation.
  /// Optimized: only processes when there are active fires, region-based sync.
  /// </summary>
  public class VegetationManager : MonoBehaviour {
    [Header("Settings")]
    [Tooltip("How often to sync changes to terrain (seconds)")]
    [SerializeField] private float syncInterval = 0.1f;
    
    [Header("Fire Settings")]
    [Tooltip("Enable fire spreading")]
    [SerializeField] private bool enableFire = true;
    
    [Tooltip("Fire tick interval (seconds)")]
    [SerializeField] private float fireTickInterval = 0.5f;
    
    [Tooltip("Burn damage per tick (0-1)")]
    [SerializeField, Range(0.01f, 0.5f)] private float burnRate = 0.1f;
    
    [Tooltip("Chance to spread to neighbor per tick")]
    [SerializeField, Range(0f, 1f)] private float spreadChance = 0.3f;
    
    [Tooltip("How many spread attempts per burning cell per tick (1=normal, 4=fast, 8=very fast)")]
    [SerializeField, Range(1, 8)] private int spreadIterations = 1;
    
    [Header("Fire Effects")]
    [Tooltip("Fire particle prefab")]
    [SerializeField] private GameObject firePrefab;
    
    [Tooltip("Initial pool size for fire effects")]
    [SerializeField] private int firePoolSize = 50;
    
    [Tooltip("Max concurrent fire effects (performance limit)")]
    [SerializeField] private int maxFireEffects = 200;
    
    [Tooltip("Spawn one fire effect per NxN grid cells (reduces visual clutter)")]
    [SerializeField, Range(1, 8)] private int fireEffectSpacing = 4;

    private VegetationGrid _grid;
    private Terrain _terrain;
    private float _lastSyncTime;
    private float _lastFireTick;
    
    private FireEffectPool _firePool;
    
    // Cache for region checks
    private readonly HashSet<Vector2Int> _tempRegions = new();

    public static VegetationManager Instance { get; private set; }
    public VegetationGrid Grid => _grid;
    public bool IsInitialized => _grid != null;
    public int ActiveFireEffects => _firePool?.ActiveCount ?? 0;
    public int BurningCells => _grid?.BurningCellCount ?? 0;

    private void Awake() {
      if (Instance != null && Instance != this) {
        Destroy(gameObject);
        return;
      }
      Instance = this;
    }

    /// <summary>
    /// Initialize vegetation grid. Called by WorldModule after generation completes.
    /// </summary>
    public void Initialize(Terrain terrain = null) {
      _terrain = terrain;
      if (_terrain == null) {
        _terrain = GetComponent<Terrain>();
      }
      if (_terrain == null) {
        _terrain = Terrain.activeTerrain;
      }
      
      if (_terrain == null) {
        Debug.LogWarning("[VegetationManager] No terrain found");
        return;
      }
      
      if (_terrain.terrainData.detailPrototypes.Length == 0) {
        Debug.Log("[VegetationManager] No detail prototypes on terrain, skipping init");
        return;
      }
      
      _grid = new VegetationGrid(_terrain);
      _lastSyncTime = Time.time;
      _lastFireTick = Time.time;
      
      // Initialize fire effect pool
      if (firePrefab != null) {
        _firePool?.Destroy();
        _firePool = new FireEffectPool(firePrefab, transform, firePoolSize);
      }
      
      Debug.Log("[VegetationManager] Initialized");
    }

    /// <summary>
    /// Re-initialize grid (call after world regeneration).
    /// </summary>
    public void Reinitialize(Terrain terrain = null) {
      _firePool?.ClearAll();
      _grid = null;
      Initialize(terrain);
    }

    /// <summary>
    /// Shutdown and cleanup.
    /// </summary>
    public void Shutdown() {
      _firePool?.Destroy();
      _firePool = null;
      _grid = null;
    }

    private void Update() {
      if (_grid == null) return;
      
      // Only process fire if there are active fires
      if (enableFire && _grid.BurningCellCount > 0) {
        if (Time.time - _lastFireTick >= fireTickInterval) {
          _lastFireTick = Time.time;
          ProcessFire();
        }
      }
      
      // Periodic sync (only if dirty)
      if (_grid.HasDirtyCells) {
        if (Time.time - _lastSyncTime >= syncInterval) {
          _lastSyncTime = Time.time;
          _grid.SyncToTerrain();
        }
      }
    }

    private void ProcessFire() {
      // Process fire spread and burn
      _grid.ProcessFireTick(burnRate, spreadChance, spreadIterations);
      
      // Update fire visual effects
      UpdateFireEffects();
    }

    private void UpdateFireEffects() {
      if (_firePool == null) return;
      
      var burningCells = _grid.GetBurningCells();
      var burningSet = burningCells as HashSet<Vector2Int> ?? new HashSet<Vector2Int>(burningCells);
      
      // Build set of regions that already have effects
      _tempRegions.Clear();
      foreach (var coord in _firePool.GetActivePositions()) {
        _tempRegions.Add(GetRegion(coord));
      }
      
      // Spawn new fire effects (one per region, respect limit)
      foreach (var coord in burningCells) {
        if (_firePool.ActiveCount >= maxFireEffects) break;
        
        var region = GetRegion(coord);
        if (_tempRegions.Contains(region)) continue;
        
        var worldPos = _grid.GridToWorld(coord);
        if (_firePool.SpawnAt(coord, worldPos)) {
          _tempRegions.Add(region);
        }
      }
      
      // Remove effects for cells no longer burning
      var toRemove = new List<Vector2Int>();
      foreach (var coord in _firePool.GetActivePositions()) {
        if (!burningSet.Contains(coord)) {
          toRemove.Add(coord);
        }
      }
      
      foreach (var coord in toRemove) {
        _firePool.RemoveAt(coord);
      }
    }
    
    private Vector2Int GetRegion(Vector2Int coord) {
      return new Vector2Int(coord.x / fireEffectSpacing, coord.y / fireEffectSpacing);
    }

    private void OnDestroy() {
      _firePool?.Destroy();
      
      if (Instance == this) {
        Instance = null;
      }
    }

    #region Public API

    /// <summary>
    /// Clear vegetation at world position.
    /// </summary>
    public void ClearVegetationAt(Vector3 worldPos) {
      if (_grid == null) return;
      
      var coord = _grid.WorldToGrid(worldPos);
      _firePool?.RemoveAt(coord);
      _grid.ClearAt(worldPos);
    }

    /// <summary>
    /// Clear vegetation in radius.
    /// </summary>
    public void ClearVegetationInRadius(Vector3 worldPos, float radius) {
      _grid?.ClearInRadius(worldPos, radius);
    }

    /// <summary>
    /// Start fire at world position.
    /// </summary>
    public void StartFireAt(Vector3 worldPos) {
      if (_grid == null) return;
      
      _grid.IgniteAt(worldPos);
      
      // Immediately spawn effect (if region doesn't have one)
      if (_firePool != null && _firePool.ActiveCount < maxFireEffects) {
        var coord = _grid.WorldToGrid(worldPos);
        if (!HasEffectInRegion(coord)) {
          _firePool.SpawnAt(coord, worldPos);
        }
      }
    }
    
    private bool HasEffectInRegion(Vector2Int coord) {
      var region = GetRegion(coord);
      foreach (var existingCoord in _firePool.GetActivePositions()) {
        if (GetRegion(existingCoord) == region) return true;
      }
      return false;
    }

    /// <summary>
    /// Start fire in radius.
    /// </summary>
    public void StartFireInRadius(Vector3 worldPos, float radius) {
      if (_grid == null) return;
      
      var center = _grid.WorldToGrid(worldPos);
      var cellSize = _terrain.terrainData.size.x / _grid.Resolution;
      var gridRadius = Mathf.CeilToInt(radius / cellSize);
      var radiusSqr = radius * radius;
      
      // Track which regions got effects in this call
      var regionsWithEffects = new HashSet<Vector2Int>();
      
      // Pre-populate with existing effects
      if (_firePool != null) {
        foreach (var existingCoord in _firePool.GetActivePositions()) {
          regionsWithEffects.Add(GetRegion(existingCoord));
        }
      }
      
      for (var dx = -gridRadius; dx <= gridRadius; dx++) {
        for (var dz = -gridRadius; dz <= gridRadius; dz++) {
          var coord = new Vector2Int(center.x + dx, center.y + dz);
          if (!_grid.IsValidCoord(coord)) continue;
          
          var cellWorld = _grid.GridToWorld(coord);
          var distSqr = (worldPos.x - cellWorld.x) * (worldPos.x - cellWorld.x) + 
                        (worldPos.z - cellWorld.z) * (worldPos.z - cellWorld.z);
          if (distSqr > radiusSqr) continue;
          
          ref var cell = ref _grid.GetCell(coord);
          if (!cell.HasVegetation || cell.isOnFire) continue;
          
          _grid.IgniteAt(cellWorld);
          
          // Spawn effect (one per region)
          if (_firePool != null && _firePool.ActiveCount < maxFireEffects) {
            var region = GetRegion(coord);
            if (!regionsWithEffects.Contains(region)) {
              _firePool.SpawnAt(coord, cellWorld);
              regionsWithEffects.Add(region);
            }
          }
        }
      }
    }

    /// <summary>
    /// Extinguish fire at world position.
    /// </summary>
    public void ExtinguishAt(Vector3 worldPos) {
      if (_grid == null) return;
      
      var coord = _grid.WorldToGrid(worldPos);
      _grid.ExtinguishAt(coord);
      _firePool?.RemoveAt(coord);
    }

    /// <summary>
    /// Extinguish all fires.
    /// </summary>
    public void ExtinguishAll() {
      if (_grid == null) return;
      
      _grid.ExtinguishAll();
      _firePool?.ClearAll();
    }

    /// <summary>
    /// Check if there's vegetation at position.
    /// </summary>
    public bool HasVegetationAt(Vector3 worldPos) {
      return _grid?.HasVegetationAt(worldPos) ?? false;
    }

    /// <summary>
    /// Check if position is on fire.
    /// </summary>
    public bool IsOnFireAt(Vector3 worldPos) {
      return _grid?.IsOnFireAt(worldPos) ?? false;
    }

    /// <summary>
    /// Force immediate sync to terrain.
    /// </summary>
    public void ForceSync() {
      _grid?.SyncToTerrain();
    }

    #endregion

    #region Debug

    [FoldoutGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private string Status => IsInitialized 
      ? $"Grid: {_grid.Resolution}x{_grid.Resolution}, Cell: {CellSizeMeters:F2}m, Fires: {BurningCells}, Effects: {ActiveFireEffects}" 
      : "Not initialized";
    
    [FoldoutGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private float CellSizeMeters => _terrain != null && _grid != null 
      ? _terrain.terrainData.size.x / _grid.Resolution 
      : 0f;
    
    [FoldoutGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private float FireEffectAreaMeters => CellSizeMeters * fireEffectSpacing;

    [Button("Initialize Now"), FoldoutGroup("Debug")]
    [EnableIf("@!IsInitialized")]
    private void DebugInitialize() {
      Initialize();
    }

    [Button("Clear All Vegetation"), FoldoutGroup("Debug")]
    [EnableIf("IsInitialized")]
    private void DebugClearAll() {
      if (_grid == null) return;
      
      // Clear only cells with vegetation (not entire grid)
      for (var x = 0; x < _grid.Resolution; x++) {
        for (var z = 0; z < _grid.Resolution; z++) {
          ref var cell = ref _grid.GetCell(new Vector2Int(x, z));
          if (cell.HasVegetation) {
            cell.Clear();
          }
        }
      }
      _grid.SyncToTerrain();
      _firePool?.ClearAll();
      Debug.Log("[VegetationManager] Cleared all vegetation");
    }

    [Button("Start Random Fire"), FoldoutGroup("Debug")]
    [EnableIf("IsInitialized")]
    private void DebugStartRandomFire() {
      if (_grid == null) return;
      
      // Find a cell with vegetation
      for (var attempt = 0; attempt < 100; attempt++) {
        var x = Random.Range(0, _grid.Resolution);
        var z = Random.Range(0, _grid.Resolution);
        var coord = new Vector2Int(x, z);
        
        ref var cell = ref _grid.GetCell(coord);
        if (cell.HasVegetation && !cell.isOnFire) {
          var worldPos = _grid.GridToWorld(coord);
          StartFireAt(worldPos);
          Debug.Log($"[VegetationManager] Started fire at {worldPos}");
          return;
        }
      }
      
      Debug.LogWarning("[VegetationManager] Could not find vegetated cell for fire");
    }

    [Button("Start Fire Radius (10m)"), FoldoutGroup("Debug")]
    [EnableIf("IsInitialized")]
    private void DebugStartFireRadius() {
      var center = _terrain != null ? _terrain.transform.position + _terrain.terrainData.size * 0.5f : Vector3.zero;
      StartFireInRadius(center, 10f);
      Debug.Log($"[VegetationManager] Started fire in 10m radius at {center}");
    }

    [Button("Extinguish All"), FoldoutGroup("Debug")]
    [EnableIf("IsInitialized")]
    private void DebugExtinguishAll() {
      ExtinguishAll();
      Debug.Log("[VegetationManager] Extinguished all fires");
    }

    [Button("Reload From Terrain"), FoldoutGroup("Debug")]
    [EnableIf("IsInitialized")]
    private void DebugReloadFromTerrain() {
      _firePool?.ClearAll();
      _grid?.LoadFromTerrain();
      Debug.Log("[VegetationManager] Reloaded from terrain");
    }

    #endregion
  }
}
