using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Analyzes terrain heightmap to detect features like cliff edges, valleys, peaks.
  /// Uses Sobel operator for edge detection.
  /// </summary>
  public class TerrainFeatureMap {
    private readonly int _resolution;
    private readonly float _terrainWidth;
    private readonly float _terrainHeight;
    private readonly Vector3 _terrainPos;

    private readonly float[,] _edgeStrength;
    private readonly float[,] _normalizedHeight;

    private const float CLIFF_THRESHOLD = 0.10f;
    private const float HEIGHT_DIFF_THRESHOLD = 0.015f; // Min height difference to count as edge/base
    private const float VALLEY_HEIGHT_THRESHOLD = 0.3f;
    private const int SEARCH_RADIUS = 3;

    public int resolution => _resolution;
    public float maxEdgeStrength { get; private set; }

    private TerrainFeatureMap(Terrain terrain, int resolution) {
      _resolution = resolution;
      var data = terrain.terrainData;
      _terrainWidth = data.size.x;
      _terrainHeight = data.size.z;
      _terrainPos = terrain.transform.position;

      _edgeStrength = new float[resolution, resolution];
      _normalizedHeight = new float[resolution, resolution];
    }

    public static TerrainFeatureMap Generate(Terrain terrain, int resolution = 256) {
      if (terrain == null) return null;

      var map = new TerrainFeatureMap(terrain, resolution);
      var data = terrain.terrainData;
      var heightmapRes = data.heightmapResolution;

      var heights = new float[resolution, resolution];
      for (var z = 0; z < resolution; z++) {
        for (var x = 0; x < resolution; x++) {
          var normX = (float)x / (resolution - 1);
          var normZ = (float)z / (resolution - 1);

          var hmX = Mathf.RoundToInt(normX * (heightmapRes - 1));
          var hmZ = Mathf.RoundToInt(normZ * (heightmapRes - 1));

          heights[z, x] = data.GetHeight(hmX, hmZ);
          map._normalizedHeight[z, x] = heights[z, x] / data.size.y;
        }
      }

      ApplySobel(heights, resolution, map._edgeStrength);
      map.maxEdgeStrength = NormalizeEdgeStrength(map._edgeStrength, resolution);

      // Count for debug
      var cliffCount = 0;
      var edgeCount = 0;
      var baseCount = 0;
      for (var z = SEARCH_RADIUS; z < resolution - SEARCH_RADIUS; z++) {
        for (var x = SEARCH_RADIUS; x < resolution - SEARCH_RADIUS; x++) {
          if (map._edgeStrength[z, x] > CLIFF_THRESHOLD) {
            cliffCount++;
            if (map.IsLocalHigh(x, z)) edgeCount++;
            if (map.IsLocalLow(x, z)) baseCount++;
          }
        }
      }

      Debug.Log($"[TerrainFeatureMap] Generated ({resolution}x{resolution}), " +
                $"maxEdge: {map.maxEdgeStrength:F3}, cliffs: {cliffCount}, edges: {edgeCount}, bases: {baseCount}");
      
      return map;
    }

    private static void ApplySobel(float[,] heights, int res, float[,] strength) {
      var sobelX = new float[,] {
        { -1, 0, 1 },
        { -2, 0, 2 },
        { -1, 0, 1 }
      };
      var sobelZ = new float[,] {
        { -1, -2, -1 },
        {  0,  0,  0 },
        {  1,  2,  1 }
      };

      for (var z = 1; z < res - 1; z++) {
        for (var x = 1; x < res - 1; x++) {
          var gx = 0f;
          var gz = 0f;

          for (var kz = -1; kz <= 1; kz++) {
            for (var kx = -1; kx <= 1; kx++) {
              var h = heights[z + kz, x + kx];
              gx += h * sobelX[kz + 1, kx + 1];
              gz += h * sobelZ[kz + 1, kx + 1];
            }
          }

          strength[z, x] = Mathf.Sqrt(gx * gx + gz * gz);
        }
      }
    }

    private static float NormalizeEdgeStrength(float[,] strength, int res) {
      var max = 0f;
      for (var z = 0; z < res; z++) {
        for (var x = 0; x < res; x++) {
          if (strength[z, x] > max) max = strength[z, x];
        }
      }

      if (max > 0.001f) {
        for (var z = 0; z < res; z++) {
          for (var x = 0; x < res; x++) {
            strength[z, x] /= max;
          }
        }
      }

      return max;
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Check if this cell is higher than at least one neighbor within radius.
    /// </summary>
    private bool IsLocalHigh(int x, int z) {
      var myHeight = _normalizedHeight[z, x];
      
      for (var dz = -SEARCH_RADIUS; dz <= SEARCH_RADIUS; dz++) {
        for (var dx = -SEARCH_RADIUS; dx <= SEARCH_RADIUS; dx++) {
          if (dx == 0 && dz == 0) continue;
          
          var nx = x + dx;
          var nz = z + dz;
          
          if (nx < 0 || nx >= _resolution || nz < 0 || nz >= _resolution) continue;
          
          if (_normalizedHeight[nz, nx] < myHeight - HEIGHT_DIFF_THRESHOLD) {
            return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Check if this cell is lower than at least one neighbor within radius.
    /// </summary>
    private bool IsLocalLow(int x, int z) {
      var myHeight = _normalizedHeight[z, x];
      
      for (var dz = -SEARCH_RADIUS; dz <= SEARCH_RADIUS; dz++) {
        for (var dx = -SEARCH_RADIUS; dx <= SEARCH_RADIUS; dx++) {
          if (dx == 0 && dz == 0) continue;
          
          var nx = x + dx;
          var nz = z + dz;
          
          if (nx < 0 || nx >= _resolution || nz < 0 || nz >= _resolution) continue;
          
          if (_normalizedHeight[nz, nx] > myHeight + HEIGHT_DIFF_THRESHOLD) {
            return true;
          }
        }
      }
      return false;
    }

    // ═══════════════════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════════════════

    public float GetEdgeStrength(Vector3 worldPos) {
      var (x, z) = WorldToGrid(new Vector2(worldPos.x, worldPos.z));
      if (x < 0 || x >= _resolution || z < 0 || z >= _resolution) return 0f;
      return _edgeStrength[z, x];
    }

    /// <summary>
    /// Check if position is at TOP of a cliff (flat area with steep drop nearby).
    /// </summary>
    public bool IsCliffEdge(Vector2 worldPos) {
      var (x, z) = WorldToGrid(worldPos);
      if (x < SEARCH_RADIUS || x >= _resolution - SEARCH_RADIUS || 
          z < SEARCH_RADIUS || z >= _resolution - SEARCH_RADIUS) return false;

      // This cell must be relatively flat (low edge strength)
      if (_edgeStrength[z, x] > CLIFF_THRESHOLD * 0.5f) return false;

      var myHeight = _normalizedHeight[z, x];
      
      // Look for steep drop nearby (neighbor with high edge AND lower height)
      for (var dz = -SEARCH_RADIUS; dz <= SEARCH_RADIUS; dz++) {
        for (var dx = -SEARCH_RADIUS; dx <= SEARCH_RADIUS; dx++) {
          if (dx == 0 && dz == 0) continue;
          
          var nx = x + dx;
          var nz = z + dz;
          
          if (nx < 0 || nx >= _resolution || nz < 0 || nz >= _resolution) continue;
          
          // Neighbor is on steep slope AND lower than us
          if (_edgeStrength[nz, nx] > CLIFF_THRESHOLD && 
              _normalizedHeight[nz, nx] < myHeight - HEIGHT_DIFF_THRESHOLD) {
            return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Check if position is at BOTTOM of a cliff (flat area with steep rise nearby).
    /// </summary>
    public bool IsCliffBase(Vector2 worldPos) {
      var (x, z) = WorldToGrid(worldPos);
      if (x < SEARCH_RADIUS || x >= _resolution - SEARCH_RADIUS || 
          z < SEARCH_RADIUS || z >= _resolution - SEARCH_RADIUS) return false;

      // This cell must be relatively flat (low edge strength)
      if (_edgeStrength[z, x] > CLIFF_THRESHOLD * 0.5f) return false;

      var myHeight = _normalizedHeight[z, x];
      
      // Look for steep rise nearby (neighbor with high edge AND higher height)
      for (var dz = -SEARCH_RADIUS; dz <= SEARCH_RADIUS; dz++) {
        for (var dx = -SEARCH_RADIUS; dx <= SEARCH_RADIUS; dx++) {
          if (dx == 0 && dz == 0) continue;
          
          var nx = x + dx;
          var nz = z + dz;
          
          if (nx < 0 || nx >= _resolution || nz < 0 || nz >= _resolution) continue;
          
          // Neighbor is on steep slope AND higher than us
          if (_edgeStrength[nz, nx] > CLIFF_THRESHOLD && 
              _normalizedHeight[nz, nx] > myHeight + HEIGHT_DIFF_THRESHOLD) {
            return true;
          }
        }
      }
      return false;
    }

    public bool IsValley(Vector2 worldPos) {
      var (x, z) = WorldToGrid(worldPos);
      if (x < 1 || x >= _resolution - 1 || z < 1 || z >= _resolution - 1) return false;

      var h = _normalizedHeight[z, x];
      if (h > VALLEY_HEIGHT_THRESHOLD) return false;

      var neighborAvg = (
        _normalizedHeight[z - 1, x] +
        _normalizedHeight[z + 1, x] +
        _normalizedHeight[z, x - 1] +
        _normalizedHeight[z, x + 1]
      ) / 4f;

      return h <= neighborAvg;
    }

    public bool CheckPlacement(Vector3 worldPos, Biomes.ScatterPlacement placement) {
      var pos2D = new Vector2(worldPos.x, worldPos.z);
      return placement switch {
        Biomes.ScatterPlacement.CliffEdge => IsCliffEdge(pos2D),
        Biomes.ScatterPlacement.CliffBase => IsCliffBase(pos2D),
        Biomes.ScatterPlacement.Valley => IsValley(pos2D),
        _ => true
      };
    }

    private (int x, int z) WorldToGrid(Vector2 worldPos) {
      var localX = worldPos.x - _terrainPos.x;
      var localZ = worldPos.y - _terrainPos.z;

      var normX = localX / _terrainWidth;
      var normZ = localZ / _terrainHeight;

      var x = Mathf.RoundToInt(normX * (_resolution - 1));
      var z = Mathf.RoundToInt(normZ * (_resolution - 1));

      return (x, z);
    }

    // ═══════════════════════════════════════════════════════════════
    // DEBUG
    // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    public int CountPlacementCells(Biomes.ScatterPlacement placement) {
      var count = 0;
      var cellWidth = _terrainWidth / _resolution;
      var cellHeight = _terrainHeight / _resolution;
      
      for (var z = SEARCH_RADIUS; z < _resolution - SEARCH_RADIUS; z++) {
        for (var x = SEARCH_RADIUS; x < _resolution - SEARCH_RADIUS; x++) {
          var worldX = _terrainPos.x + (x + 0.5f) * cellWidth;
          var worldZ = _terrainPos.z + (z + 0.5f) * cellHeight;
          
          if (CheckPlacement(new Vector3(worldX, 0, worldZ), placement)) {
            count++;
          }
        }
      }
      return count;
    }
    
    /// <summary>
    /// Get all world positions that match the placement type.
    /// Used for targeted scatter placement.
    /// </summary>
    public List<Vector3> GetValidPositions(Biomes.ScatterPlacement placement) {
      var result = new List<Vector3>();
      var cellWidth = _terrainWidth / _resolution;
      var cellHeight = _terrainHeight / _resolution;
      
      for (var z = SEARCH_RADIUS; z < _resolution - SEARCH_RADIUS; z++) {
        for (var x = SEARCH_RADIUS; x < _resolution - SEARCH_RADIUS; x++) {
          var worldX = _terrainPos.x + (x + 0.5f) * cellWidth;
          var worldZ = _terrainPos.z + (z + 0.5f) * cellHeight;
          var pos = new Vector3(worldX, 0, worldZ);
          
          if (CheckPlacement(pos, placement)) {
            result.Add(pos);
          }
        }
      }
      return result;
    }
#endif
  }
}
