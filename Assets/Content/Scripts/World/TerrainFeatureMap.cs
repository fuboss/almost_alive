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

    // Feature maps
    private readonly float[,] _edgeStrength;     // 0-1 cliff intensity
    private readonly Vector2[,] _gradientDir;    // slope direction (normalized)
    private readonly float[,] _normalizedHeight; // 0-1 height

    // Thresholds
    private const float CLIFF_EDGE_THRESHOLD = 0.4f;
    private const float CLIFF_BASE_THRESHOLD = 0.3f;
    private const float VALLEY_HEIGHT_THRESHOLD = 0.25f;
    private const float PEAK_HEIGHT_THRESHOLD = 0.75f;

    public int resolution => _resolution;

    private TerrainFeatureMap(Terrain terrain, int resolution) {
      _resolution = resolution;
      var data = terrain.terrainData;
      _terrainWidth = data.size.x;
      _terrainHeight = data.size.z;
      _terrainPos = terrain.transform.position;

      _edgeStrength = new float[resolution, resolution];
      _gradientDir = new Vector2[resolution, resolution];
      _normalizedHeight = new float[resolution, resolution];
    }

    /// <summary>
    /// Generate feature map from terrain heightmap.
    /// </summary>
    public static TerrainFeatureMap Generate(Terrain terrain, int resolution = 128) {
      if (terrain == null) return null;

      var map = new TerrainFeatureMap(terrain, resolution);
      var data = terrain.terrainData;
      var heightmapRes = data.heightmapResolution;

      // Sample heights at feature map resolution
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

      // Sobel edge detection
      ApplySobel(heights, resolution, map._edgeStrength, map._gradientDir);

      // Normalize edge strength
      NormalizeEdgeStrength(map._edgeStrength, resolution);

      Debug.Log($"[TerrainFeatureMap] Generated ({resolution}x{resolution})");
      return map;
    }

    private static void ApplySobel(float[,] heights, int res, float[,] strength, Vector2[,] direction) {
      // Sobel kernels
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
          direction[z, x] = new Vector2(gx, gz).normalized;
        }
      }
    }

    private static void NormalizeEdgeStrength(float[,] strength, int res) {
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
    }

    // ═══════════════════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get edge strength at world position (0-1).
    /// </summary>
    public float GetEdgeStrength(Vector2 worldPos) {
      var (x, z) = WorldToGrid(worldPos);
      if (x < 0 || x >= _resolution || z < 0 || z >= _resolution) return 0f;
      return _edgeStrength[z, x];
    }

    /// <summary>
    /// Get edge strength at world position (0-1).
    /// </summary>
    public float GetEdgeStrength(Vector3 worldPos) {
      return GetEdgeStrength(new Vector2(worldPos.x, worldPos.z));
    }

    /// <summary>
    /// Get gradient direction at world position.
    /// </summary>
    public Vector2 GetGradientDirection(Vector2 worldPos) {
      var (x, z) = WorldToGrid(worldPos);
      if (x < 0 || x >= _resolution || z < 0 || z >= _resolution) return Vector2.zero;
      return _gradientDir[z, x];
    }

    /// <summary>
    /// Check if position is at top of a cliff.
    /// </summary>
    public bool IsCliffEdge(Vector2 worldPos) {
      var (x, z) = WorldToGrid(worldPos);
      if (x < 1 || x >= _resolution - 1 || z < 1 || z >= _resolution - 1) return false;

      var edge = _edgeStrength[z, x];
      if (edge < CLIFF_EDGE_THRESHOLD) return false;

      // Check if we're on the "high" side of the gradient
      var grad = _gradientDir[z, x];
      var checkX = x - Mathf.RoundToInt(grad.x);
      var checkZ = z - Mathf.RoundToInt(grad.y);

      if (checkX < 0 || checkX >= _resolution || checkZ < 0 || checkZ >= _resolution) return false;

      return _normalizedHeight[z, x] > _normalizedHeight[checkZ, checkX];
    }

    /// <summary>
    /// Check if position is at bottom of a cliff.
    /// </summary>
    public bool IsCliffBase(Vector2 worldPos) {
      var (x, z) = WorldToGrid(worldPos);
      if (x < 1 || x >= _resolution - 1 || z < 1 || z >= _resolution - 1) return false;

      var edge = _edgeStrength[z, x];
      if (edge < CLIFF_BASE_THRESHOLD) return false;

      // Check if we're on the "low" side of the gradient
      var grad = _gradientDir[z, x];
      var checkX = x - Mathf.RoundToInt(grad.x);
      var checkZ = z - Mathf.RoundToInt(grad.y);

      if (checkX < 0 || checkX >= _resolution || checkZ < 0 || checkZ >= _resolution) return false;

      return _normalizedHeight[z, x] < _normalizedHeight[checkZ, checkX];
    }

    /// <summary>
    /// Check if position is in a valley (local low point).
    /// </summary>
    public bool IsValley(Vector2 worldPos) {
      var (x, z) = WorldToGrid(worldPos);
      if (x < 1 || x >= _resolution - 1 || z < 1 || z >= _resolution - 1) return false;

      var h = _normalizedHeight[z, x];
      if (h > VALLEY_HEIGHT_THRESHOLD) return false;

      // Check if lower than neighbors
      var neighborAvg = (
        _normalizedHeight[z - 1, x] +
        _normalizedHeight[z + 1, x] +
        _normalizedHeight[z, x - 1] +
        _normalizedHeight[z, x + 1]
      ) / 4f;

      return h <= neighborAvg;
    }

    /// <summary>
    /// Check if position is at a peak (local high point).
    /// </summary>
    public bool IsPeak(Vector2 worldPos) {
      var (x, z) = WorldToGrid(worldPos);
      if (x < 1 || x >= _resolution - 1 || z < 1 || z >= _resolution - 1) return false;

      var h = _normalizedHeight[z, x];
      if (h < PEAK_HEIGHT_THRESHOLD) return false;

      // Check if higher than neighbors
      var neighborAvg = (
        _normalizedHeight[z - 1, x] +
        _normalizedHeight[z + 1, x] +
        _normalizedHeight[z, x - 1] +
        _normalizedHeight[z, x + 1]
      ) / 4f;

      return h >= neighborAvg;
    }

    /// <summary>
    /// Check placement type at position.
    /// </summary>
    public bool CheckPlacement(Vector3 worldPos, Biomes.ScatterPlacement placement) {
      var pos2D = new Vector2(worldPos.x, worldPos.z);
      return placement switch {
        Biomes.ScatterPlacement.CliffEdge => IsCliffEdge(pos2D),
        Biomes.ScatterPlacement.CliffBase => IsCliffBase(pos2D),
        Biomes.ScatterPlacement.Valley => IsValley(pos2D),
        _ => true // Any, FlatOnly, SlopeOnly, CliffOnly handled by slope checks
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
    /// <summary>
    /// Draw gizmos for feature map visualization.
    /// </summary>
    public void DrawGizmos(float y = 0f, bool drawEdges = true, bool drawValleys = false) {
      var cellWidth = _terrainWidth / _resolution;
      var cellHeight = _terrainHeight / _resolution;

      for (var z = 0; z < _resolution; z++) {
        for (var x = 0; x < _resolution; x++) {
          var worldX = _terrainPos.x + (x + 0.5f) * cellWidth;
          var worldZ = _terrainPos.z + (z + 0.5f) * cellHeight;
          var pos = new Vector3(worldX, y, worldZ);

          if (drawEdges && _edgeStrength[z, x] > CLIFF_EDGE_THRESHOLD) {
            Gizmos.color = new Color(1f, 0.5f, 0f, _edgeStrength[z, x]);
            Gizmos.DrawWireCube(pos, new Vector3(cellWidth * 0.8f, 0.5f, cellHeight * 0.8f));
          }

          if (drawValleys && _normalizedHeight[z, x] < VALLEY_HEIGHT_THRESHOLD) {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawCube(pos, new Vector3(cellWidth * 0.5f, 0.3f, cellHeight * 0.5f));
          }
        }
      }
    }
#endif
  }
}
