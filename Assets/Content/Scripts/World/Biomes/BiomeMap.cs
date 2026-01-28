using System;
using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.World.Biomes {
  /// <summary>
  /// Runtime biome map generated from Voronoi diagram.
  /// Uses noise-distorted distances for organic, wavy borders.
  /// </summary>
  public class BiomeMap {
    private readonly List<BiomeCell> _cells = new();
    private readonly Dictionary<BiomeType, BiomeSO> _biomeData = new();
    private readonly float _blendDistance;
    private readonly Bounds _bounds;
    private readonly float _noiseOffset;

    // Domain warping settings
    private readonly bool _useWarping;
    private readonly float _warpStrength;
    private readonly float _warpScale;
    private readonly int _warpOctaves;

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

    public BiomeMap(Bounds bounds, float blendDistance, int seed, 
                    bool useWarping = true, float warpStrength = 20f, 
                    float warpScale = 0.02f, int warpOctaves = 2) {
      _bounds = bounds;
      _blendDistance = blendDistance;
      _noiseOffset = seed * 137.5f;
      
      _useWarping = useWarping;
      _warpStrength = warpStrength;
      _warpScale = warpScale;
      _warpOctaves = warpOctaves;
    }

    public void RegisterBiome(BiomeSO biome) {
      if (biome != null) {
        _biomeData[biome.type] = biome;
      }
    }

    public void AddCell(Vector2 center, BiomeType type, int biomeIndex) {
      _cells.Add(new BiomeCell(center, type, biomeIndex));
    }

    public BiomeType GetBiomeAt(Vector3 worldPos) {
      return GetBiomeAt(new Vector2(worldPos.x, worldPos.z));
    }

    public BiomeType GetBiomeAt(Vector2 pos) {
      var (_, cell) = FindNearestCellWithNoise(pos);
      return cell.type;
    }

    public BiomeSO GetBiomeDataAt(Vector3 worldPos) {
      var type = GetBiomeAt(worldPos);
      return _biomeData.GetValueOrDefault(type);
    }

    public BiomeQuery QueryBiome(Vector3 worldPos) {
      return QueryBiome(new Vector2(worldPos.x, worldPos.z));
    }

    /// <summary>
    /// Query biome with smooth blend. Uses noise-distorted distances for organic borders.
    /// </summary>
    public BiomeQuery QueryBiome(Vector2 pos) {
      if (_cells.Count == 0) return default;

      var (dist1, cell1, dist2, cell2) = FindTwoNearestCellsWithNoise(pos);

      var primaryData = _biomeData.GetValueOrDefault(cell1.type);

      if (_cells.Count == 1) {
        return new BiomeQuery(
          cell1.type, primaryData, 1f,
          cell1.type, primaryData, 0f,
          cell1.center, dist1
        );
      }

      var distDiff = dist2 - dist1;
      
      if (distDiff >= _blendDistance) {
        return new BiomeQuery(
          cell1.type, primaryData, 1f,
          cell1.type, primaryData, 0f,
          cell1.center, dist1
        );
      }

      // Quintic smoothstep for C2 continuity
      var t = distDiff / _blendDistance;
      var smooth = t * t * t * (t * (t * 6f - 15f) + 10f);
      
      var primaryWeight = 0.5f + smooth * 0.5f;
      var secondaryWeight = 1f - primaryWeight;

      var secondaryData = _biomeData.GetValueOrDefault(cell2.type);

      return new BiomeQuery(
        cell1.type, primaryData, primaryWeight,
        cell2.type, secondaryData, secondaryWeight,
        cell1.center, dist1
      );
    }

    /// <summary>
    /// Get noise-distorted distance to cell using domain warping.
    /// Multi-octave noise creates organic, naturally wavy borders.
    /// </summary>
    private float GetNoisyDistance(Vector2 pos, BiomeCell cell) {
      var realDist = Vector2.Distance(pos, cell.center);
      
      if (!_useWarping || _warpStrength <= 0f) {
        return realDist;
      }
      
      // Domain warping: offset the sample position with noise
      var warpOffset = SampleWarpNoise(pos, cell.center);
      return realDist + warpOffset;
    }

    /// <summary>
    /// Multi-octave Perlin noise for domain warping.
    /// </summary>
    private float SampleWarpNoise(Vector2 pos, Vector2 cellCenter) {
      var total = 0f;
      var amplitude = 1f;
      var frequency = 1f;
      var maxValue = 0f;
      
      // Use cell center to make noise unique per cell
      var offsetX = _noiseOffset + cellCenter.x * 0.1f;
      var offsetY = _noiseOffset + cellCenter.y * 0.1f;
      
      for (int i = 0; i < _warpOctaves; i++) {
        var noiseX = (pos.x + offsetX) * _warpScale * frequency;
        var noiseY = (pos.y + offsetY) * _warpScale * frequency;
        
        var noise = Mathf.PerlinNoise(noiseX, noiseY);
        total += (noise - 0.5f) * 2f * amplitude;
        
        maxValue += amplitude;
        amplitude *= 0.5f;
        frequency *= 2f;
      }
      
      return (total / maxValue) * _warpStrength;
    }

    private (float distance, BiomeCell cell) FindNearestCellWithNoise(Vector2 pos) {
      var minDist = float.MaxValue;
      var nearest = _cells[0];

      foreach (var cell in _cells) {
        var dist = GetNoisyDistance(pos, cell);
        if (dist < minDist) {
          minDist = dist;
          nearest = cell;
        }
      }

      return (minDist, nearest);
    }

    private (float dist1, BiomeCell cell1, float dist2, BiomeCell cell2) FindTwoNearestCellsWithNoise(Vector2 pos) {
      var minDist1 = float.MaxValue;
      var minDist2 = float.MaxValue;
      var nearest1 = _cells[0];
      var nearest2 = _cells.Count > 1 ? _cells[1] : _cells[0];

      foreach (var cell in _cells) {
        var dist = GetNoisyDistance(pos, cell);
        
        if (dist < minDist1) {
          minDist2 = minDist1;
          nearest2 = nearest1;
          minDist1 = dist;
          nearest1 = cell;
        } else if (dist < minDist2) {
          minDist2 = dist;
          nearest2 = cell;
        }
      }

      return (minDist1, nearest1, minDist2, nearest2);
    }

    public float GetBlendWeight(Vector2 pos, BiomeType biome) {
      var query = QueryBiome(pos);
      if (query.primaryType == biome) return query.primaryWeight;
      if (query.secondaryType == biome) return query.secondaryWeight;
      return 0f;
    }

    /// <summary>
    /// Get distance to the nearest biome border.
    /// Border is where two different biomes meet (equidistant from two cell centers).
    /// </summary>
    public float GetDistanceToBorder(Vector3 worldPos) {
      return GetDistanceToBorder(new Vector2(worldPos.x, worldPos.z));
    }

    public float GetDistanceToBorder(Vector2 pos) {
      if (_cells.Count < 2) return float.MaxValue;

      var (dist1, cell1, dist2, cell2) = FindTwoNearestCellsWithNoise(pos);
      
      // If same biome type, check next neighbors
      if (cell1.type == cell2.type) {
        // Find nearest cell with different biome
        var minDistDiff = float.MaxValue;
        foreach (var cell in _cells) {
          if (cell.type != cell1.type) {
            var dist = GetNoisyDistance(pos, cell);
            var distDiff = Mathf.Abs(dist - dist1);
            if (distDiff < minDistDiff) {
              minDistDiff = distDiff;
              dist2 = dist;
            }
          }
        }
      }

      // Distance to border is half the difference between distances
      // (border is where both distances are equal)
      return Mathf.Abs(dist2 - dist1) * 0.5f;
    }

    public float GetNormalizedDistanceToCenter(Vector2 pos) {
      if (_cells.Count == 0) return 0f;

      var (dist, cell) = FindNearestCellWithNoise(pos);
      var (_, _, dist2, neighbor) = FindTwoNearestCellsWithNoise(pos);

      var cellRadius = Vector2.Distance(cell.center, neighbor.center) * 0.5f;
      if (cellRadius < 0.001f) return 0f;

      return Mathf.Clamp01(dist / cellRadius);
    }

    public void Clear() {
      _cells.Clear();
    }
  }
}
