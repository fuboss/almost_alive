using System;
using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.World.Biomes {
  /// <summary>
  /// Runtime biome map generated from Voronoi diagram.
  /// Provides biome lookups by world position with blend weights for borders.
  /// </summary>
  public class BiomeMap {
    private readonly List<BiomeCell> _cells = new();
    private readonly Dictionary<BiomeType, BiomeSO> _biomeData = new();
    private readonly float _blendDistance;
    private readonly Bounds _bounds;

    /// <summary>
    /// Single Voronoi cell representing a biome region.
    /// </summary>
    public readonly struct BiomeCell {
      public readonly Vector2 center;
      public readonly BiomeType type;
      public readonly int biomeIndex;

      public BiomeCell(Vector2 center, BiomeType type, int biomeIndex) {
        this.center = center;
        this.type = type;
        this.biomeIndex = biomeIndex;
      }
    }

    /// <summary>
    /// Result of biome query with blend weights.
    /// </summary>
    public readonly struct BiomeQuery {
      public readonly BiomeType primaryType;
      public readonly BiomeSO primaryData;
      public readonly float primaryWeight;
      
      public readonly BiomeType secondaryType;
      public readonly BiomeSO secondaryData;
      public readonly float secondaryWeight;

      public readonly Vector2 cellCenter;
      public readonly float distanceToCenter;

      public BiomeQuery(BiomeType primary, BiomeSO primaryData, float primaryWeight,
        BiomeType secondary, BiomeSO secondaryData, float secondaryWeight,
        Vector2 cellCenter, float distanceToCenter) {
        this.primaryType = primary;
        this.primaryData = primaryData;
        this.primaryWeight = primaryWeight;
        this.secondaryType = secondary;
        this.secondaryData = secondaryData;
        this.secondaryWeight = secondaryWeight;
        this.cellCenter = cellCenter;
        this.distanceToCenter = distanceToCenter;
      }

      public bool isBlending => secondaryWeight > 0.001f;
    }

    public IReadOnlyList<BiomeCell> cells => _cells;
    public Bounds bounds => _bounds;

    public BiomeMap(Bounds bounds, float blendDistance) {
      _bounds = bounds;
      _blendDistance = blendDistance;
    }

    /// <summary>
    /// Register biome data for lookups.
    /// </summary>
    public void RegisterBiome(BiomeSO biome) {
      if (biome != null) {
        _biomeData[biome.type] = biome;
      }
    }

    /// <summary>
    /// Add a Voronoi cell to the map.
    /// </summary>
    public void AddCell(Vector2 center, BiomeType type, int biomeIndex) {
      _cells.Add(new BiomeCell(center, type, biomeIndex));
    }

    /// <summary>
    /// Get biome type at world position (simple, no blending).
    /// </summary>
    public BiomeType GetBiomeAt(Vector3 worldPos) {
      return GetBiomeAt(new Vector2(worldPos.x, worldPos.z));
    }

    /// <summary>
    /// Get biome type at 2D position (simple, no blending).
    /// </summary>
    public BiomeType GetBiomeAt(Vector2 pos) {
      var (_, cell) = FindNearestCell(pos);
      return cell.type;
    }

    /// <summary>
    /// Get biome data at world position.
    /// </summary>
    public BiomeSO GetBiomeDataAt(Vector3 worldPos) {
      var type = GetBiomeAt(worldPos);
      return _biomeData.GetValueOrDefault(type);
    }

    /// <summary>
    /// Query biome with blend weights for smooth transitions.
    /// </summary>
    public BiomeQuery QueryBiome(Vector3 worldPos) {
      return QueryBiome(new Vector2(worldPos.x, worldPos.z));
    }

    /// <summary>
    /// Query biome with blend weights at 2D position.
    /// </summary>
    public BiomeQuery QueryBiome(Vector2 pos) {
      if (_cells.Count == 0) {
        return default;
      }

      // Find two nearest cells
      var (dist1, cell1) = FindNearestCell(pos);
      var (dist2, cell2) = FindSecondNearestCell(pos, cell1);

      var primaryData = _biomeData.GetValueOrDefault(cell1.type);

      // No blending if only one cell or second is too far
      if (_cells.Count == 1 || dist2 - dist1 > _blendDistance * 2) {
        return new BiomeQuery(
          cell1.type, primaryData, 1f,
          cell1.type, primaryData, 0f,
          cell1.center, dist1
        );
      }

      // Calculate blend factor based on distance difference
      var blendZone = _blendDistance;
      var distDiff = dist2 - dist1;
      
      // If we're clearly in one biome (distance diff > blend zone), no blending
      if (distDiff > blendZone) {
        return new BiomeQuery(
          cell1.type, primaryData, 1f,
          cell1.type, primaryData, 0f,
          cell1.center, dist1
        );
      }

      // Blend: when distDiff=0, we're exactly on border (50/50)
      // when distDiff=blendZone, we're at edge of blend (100/0)
      var t = distDiff / blendZone;
      var primaryWeight = 0.5f + t * 0.5f;  // 0.5 to 1.0
      var secondaryWeight = 1f - primaryWeight;

      var secondaryData = _biomeData.GetValueOrDefault(cell2.type);

      return new BiomeQuery(
        cell1.type, primaryData, primaryWeight,
        cell2.type, secondaryData, secondaryWeight,
        cell1.center, dist1
      );
    }

    /// <summary>
    /// Get blend weight for specific biome at position (0-1).
    /// Useful for splatmap generation.
    /// </summary>
    public float GetBlendWeight(Vector2 pos, BiomeType biome) {
      var query = QueryBiome(pos);
      
      if (query.primaryType == biome) return query.primaryWeight;
      if (query.secondaryType == biome) return query.secondaryWeight;
      return 0f;
    }

    /// <summary>
    /// Get normalized distance from position to nearest cell center (0-1).
    /// Useful for height profile sampling.
    /// </summary>
    public float GetNormalizedDistanceToCenter(Vector2 pos) {
      if (_cells.Count == 0) return 0f;

      var (dist, cell) = FindNearestCell(pos);
      var (_, neighbor) = FindSecondNearestCell(pos, cell);

      // Estimate cell "radius" as half distance to neighbor
      var cellRadius = Vector2.Distance(cell.center, neighbor.center) * 0.5f;
      if (cellRadius < 0.001f) return 0f;

      return Mathf.Clamp01(dist / cellRadius);
    }

    private (float distance, BiomeCell cell) FindNearestCell(Vector2 pos) {
      var minDist = float.MaxValue;
      var nearest = _cells[0];

      foreach (var cell in _cells) {
        var dist = Vector2.Distance(pos, cell.center);
        if (dist < minDist) {
          minDist = dist;
          nearest = cell;
        }
      }

      return (minDist, nearest);
    }

    private (float distance, BiomeCell cell) FindSecondNearestCell(Vector2 pos, BiomeCell exclude) {
      var minDist = float.MaxValue;
      var nearest = _cells.Count > 1 ? _cells[1] : _cells[0];

      foreach (var cell in _cells) {
        if (cell.center == exclude.center) continue;
        
        var dist = Vector2.Distance(pos, cell.center);
        if (dist < minDist) {
          minDist = dist;
          nearest = cell;
        }
      }

      return (minDist, nearest);
    }

    /// <summary>
    /// Clear all cells (for regeneration).
    /// </summary>
    public void Clear() {
      _cells.Clear();
    }
  }
}
