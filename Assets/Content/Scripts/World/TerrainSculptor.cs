using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Modifies terrain heightmap based on biome configuration.
  /// </summary>
  public static class TerrainSculptor {
    /// <summary>
    /// Apply biome-based heights to terrain.
    /// </summary>
    public static void Sculpt(Terrain terrain, BiomeMap biomeMap, int seed) {
      if (terrain == null || biomeMap == null) return;

      var terrainData = terrain.terrainData;
      var resolution = terrainData.heightmapResolution;
      var heights = terrainData.GetHeights(0, 0, resolution, resolution);

      var terrainPos = terrain.transform.position;
      var terrainSize = terrainData.size;
      var rng = new System.Random(seed);

      // Noise offsets for variation
      var noiseOffsetX = (float)rng.NextDouble() * 10000f;
      var noiseOffsetZ = (float)rng.NextDouble() * 10000f;

      for (var z = 0; z < resolution; z++) {
        for (var x = 0; x < resolution; x++) {
          var normalizedX = (float)x / (resolution - 1);
          var normalizedZ = (float)z / (resolution - 1);

          var worldX = terrainPos.x + normalizedX * terrainSize.x;
          var worldZ = terrainPos.z + normalizedZ * terrainSize.z;
          var pos2D = new Vector2(worldX, worldZ);

          var query = biomeMap.QueryBiome(pos2D);
          if (query.primaryData == null) continue;

          // Calculate height from primary biome
          var height = CalculateBiomeHeight(query.primaryData, pos2D, query.cellCenter, 
            noiseOffsetX, noiseOffsetZ, terrainSize.y);

          // Blend with secondary biome if on border
          if (query.isBlending && query.secondaryData != null) {
            var secondaryHeight = CalculateBiomeHeight(query.secondaryData, pos2D, query.cellCenter,
              noiseOffsetX, noiseOffsetZ, terrainSize.y);
            height = Mathf.Lerp(secondaryHeight, height, query.primaryWeight);
          }

          heights[z, x] = height;
        }
      }

      terrainData.SetHeights(0, 0, heights);
      Debug.Log($"[TerrainSculptor] Applied heightmap ({resolution}x{resolution})");
    }

    private static float CalculateBiomeHeight(BiomeSO biome, Vector2 worldPos, Vector2 cellCenter,
      float noiseOffsetX, float noiseOffsetZ, float terrainHeight) {
      // Distance from cell center for profile sampling
      var distToCenter = Vector2.Distance(worldPos, cellCenter);
      var normalizedDist = Mathf.Clamp01(distToCenter / 100f); // Approximate cell radius

      // Base height from profile
      var profileHeight = biome.heightProfile.Evaluate(normalizedDist);
      var baseHeight = biome.baseHeight + profileHeight * biome.heightAmplitude;

      // Add noise
      var noise = SampleNoise(
        worldPos.x + noiseOffsetX, 
        worldPos.y + noiseOffsetZ,
        biome.noiseFrequency,
        biome.noiseOctaves,
        biome.noisePersistence
      );

      // Noise adds variation on top of base height
      var finalHeight = baseHeight + noise * biome.heightAmplitude * 0.5f;

      // Normalize to 0-1 range for terrain
      return Mathf.Clamp01(finalHeight / terrainHeight);
    }

    private static float SampleNoise(float x, float z, float frequency, int octaves, float persistence) {
      var total = 0f;
      var amplitude = 1f;
      var maxValue = 0f;
      var freq = frequency;

      for (var i = 0; i < octaves; i++) {
        total += Mathf.PerlinNoise(x * freq, z * freq) * amplitude;
        maxValue += amplitude;
        amplitude *= persistence;
        freq *= 2f;
      }

      return total / maxValue; // Normalize to 0-1
    }

    /// <summary>
    /// Reset terrain to flat (for testing).
    /// </summary>
    public static void Flatten(Terrain terrain, float height = 0.1f) {
      if (terrain == null) return;

      var terrainData = terrain.terrainData;
      var resolution = terrainData.heightmapResolution;
      var heights = new float[resolution, resolution];

      for (var z = 0; z < resolution; z++) {
        for (var x = 0; x < resolution; x++) {
          heights[z, x] = height;
        }
      }

      terrainData.SetHeights(0, 0, heights);
      Debug.Log("[TerrainSculptor] Terrain flattened");
    }
  }
}
