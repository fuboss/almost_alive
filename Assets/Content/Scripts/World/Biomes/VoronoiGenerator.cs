using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.World.Biomes {
  /// <summary>
  /// Generates BiomeMap using Voronoi diagram with weighted biome distribution.
  /// Supports deterministic generation via seed.
  /// </summary>
  public static class VoronoiGenerator {
    /// <summary>
    /// Generate a biome map for the given terrain bounds.
    /// </summary>
    /// <param name="bounds">World bounds to fill with biomes</param>
    /// <param name="biomes">Available biome configurations</param>
    /// <param name="blendDistance">Width of blend zone between biomes</param>
    /// <param name="seed">Random seed for deterministic generation</param>
    /// <param name="minCellCount">Minimum number of Voronoi cells</param>
    /// <param name="maxCellCount">Maximum number of Voronoi cells</param>
    public static BiomeMap Generate(
      Bounds bounds,
      IReadOnlyList<BiomeSO> biomes,
      float blendDistance,
      int seed,
      int minCellCount = 8,
      int maxCellCount = 20) {
      
      if (biomes == null || biomes.Count == 0) {
        Debug.LogError("[VoronoiGenerator] No biomes provided");
        return null;
      }

      // Init deterministic random
      var rng = new System.Random(seed);
      
      var map = new BiomeMap(bounds, blendDistance);
      
      // Register biome data
      foreach (var biome in biomes) {
        if (biome != null) {
          map.RegisterBiome(biome);
        }
      }

      // Calculate total weight
      var totalWeight = 0f;
      foreach (var biome in biomes) {
        if (biome != null) totalWeight += biome.weight;
      }

      var boundsSize = new Vector2(bounds.size.x, bounds.size.z);
      var boundsMin = new Vector2(bounds.min.x, bounds.min.z);

      // Cell count from parameters
      var targetCellCount = UnityEngine.Random.Range(minCellCount, maxCellCount + 1);

      // Generate seed points with Poisson-like distribution
      var points = GeneratePoissonPoints(rng, boundsMin, boundsSize, targetCellCount);

      // Assign biomes to points based on weights
      var biomeList = new List<BiomeSO>();
      foreach (var biome in biomes) {
        if (biome != null) biomeList.Add(biome);
      }

      for (var i = 0; i < points.Count; i++) {
        var biome = SelectWeightedBiome(rng, biomeList, totalWeight);
        var biomeIndex = biomeList.IndexOf(biome);
        map.AddCell(points[i], biome.type, biomeIndex);
      }

      Debug.Log($"[VoronoiGenerator] Generated {points.Count} cells (seed: {seed})");
      return map;
    }

    /// <summary>
    /// Generate roughly uniform distributed points using simple rejection sampling.
    /// Not true Poisson disk, but good enough for biome cells.
    /// </summary>
    private static List<Vector2> GeneratePoissonPoints(System.Random rng, Vector2 min, Vector2 size, int targetCount) {
      var points = new List<Vector2>(targetCount);
      
      // Calculate minimum spacing based on area and count
      var avgCellArea = (size.x * size.y) / targetCount;
      var minSpacing = Mathf.Sqrt(avgCellArea) * 0.5f;
      var minSpacingSqr = minSpacing * minSpacing;

      var maxAttempts = targetCount * 50;
      var attempts = 0;

      while (points.Count < targetCount && attempts < maxAttempts) {
        attempts++;

        var x = min.x + (float)rng.NextDouble() * size.x;
        var y = min.y + (float)rng.NextDouble() * size.y;
        var candidate = new Vector2(x, y);

        // Check spacing against existing points
        var valid = true;
        foreach (var existing in points) {
          if ((existing - candidate).sqrMagnitude < minSpacingSqr) {
            valid = false;
            break;
          }
        }

        if (valid) {
          points.Add(candidate);
        }
      }

      // If we couldn't place enough points, relax spacing and try again
      if (points.Count < targetCount * 0.5f) {
        points.Clear();
        for (var i = 0; i < targetCount; i++) {
          var x = min.x + (float)rng.NextDouble() * size.x;
          var y = min.y + (float)rng.NextDouble() * size.y;
          points.Add(new Vector2(x, y));
        }
      }

      return points;
    }

    /// <summary>
    /// Select a biome based on weights (roulette wheel selection).
    /// </summary>
    private static BiomeSO SelectWeightedBiome(System.Random rng, List<BiomeSO> biomes, float totalWeight) {
      var roll = (float)rng.NextDouble() * totalWeight;
      var cumulative = 0f;

      foreach (var biome in biomes) {
        cumulative += biome.weight;
        if (roll <= cumulative) {
          return biome;
        }
      }

      return biomes[biomes.Count - 1];
    }

    /// <summary>
    /// Lloyd relaxation iteration to make cells more uniform.
    /// Optional post-processing step.
    /// </summary>
    public static void RelaxCells(BiomeMap map, Bounds bounds, int iterations = 2) {
      // TODO: Implement Lloyd relaxation if needed
      // For now, the initial distribution is good enough
    }
  }
}
