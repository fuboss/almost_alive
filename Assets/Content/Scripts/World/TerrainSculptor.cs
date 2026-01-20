using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Modifies terrain heightmap based on biome configuration.
  /// </summary>
  public static class TerrainSculptor {
    
    /// <summary>
    /// Apply biome-based heights to terrain with border smoothing.
    /// </summary>
    public static void Sculpt(Terrain terrain, BiomeMap biomeMap, int seed) {
      if (terrain == null || biomeMap == null) return;

      var terrainData = terrain.terrainData;
      var resolution = terrainData.heightmapResolution;
      var heights = terrainData.GetHeights(0, 0, resolution, resolution);

      var terrainPos = terrain.transform.position;
      var terrainSize = terrainData.size;
      var rng = new System.Random(seed);

      var noiseOffsetX = (float)rng.NextDouble() * 10000f;
      var noiseOffsetZ = (float)rng.NextDouble() * 10000f;

      // Pass 1: Generate raw heights
      for (var z = 0; z < resolution; z++) {
        for (var x = 0; x < resolution; x++) {
          var normalizedX = (float)x / (resolution - 1);
          var normalizedZ = (float)z / (resolution - 1);

          var worldX = terrainPos.x + normalizedX * terrainSize.x;
          var worldZ = terrainPos.z + normalizedZ * terrainSize.z;
          var pos2D = new Vector2(worldX, worldZ);

          var query = biomeMap.QueryBiome(pos2D);
          if (query.primaryData == null) continue;

          var height = CalculateBiomeHeight(query.primaryData, pos2D, query.cellCenter, 
            noiseOffsetX, noiseOffsetZ, terrainSize.y);

          if (query.isBlending && query.secondaryData != null) {
            var secondaryHeight = CalculateBiomeHeight(query.secondaryData, pos2D, query.cellCenter,
              noiseOffsetX, noiseOffsetZ, terrainSize.y);
            height = Mathf.Lerp(secondaryHeight, height, query.primaryWeight);
          }

          heights[z, x] = height;
        }
      }

      // Pass 2: Smooth the heightmap to remove stepping artifacts
      heights = SmoothHeightmap(heights, resolution, iterations: 3, kernelSize: 2);

      terrainData.SetHeights(0, 0, heights);
      Debug.Log($"[TerrainSculptor] Applied heightmap ({resolution}x{resolution}) with smoothing");
    }

    /// <summary>
    /// Gaussian-like smoothing to eliminate stepping artifacts.
    /// </summary>
    /// <param name="kernelSize">Radius of kernel (1=3x3, 2=5x5, 3=7x7)</param>
    private static float[,] SmoothHeightmap(float[,] heights, int resolution, int iterations, int kernelSize = 1) {
      var result = heights;
      
      for (var iter = 0; iter < iterations; iter++) {
        var smoothed = new float[resolution, resolution];
        
        for (var z = 0; z < resolution; z++) {
          for (var x = 0; x < resolution; x++) {
            var sum = 0f;
            var weightSum = 0f;
            
            for (var dz = -kernelSize; dz <= kernelSize; dz++) {
              for (var dx = -kernelSize; dx <= kernelSize; dx++) {
                var nz = z + dz;
                var nx = x + dx;
                
                if (nz < 0 || nz >= resolution || nx < 0 || nx >= resolution) continue;
                
                // Gaussian-like weight based on distance from center
                var dist = Mathf.Sqrt(dx * dx + dz * dz);
                var weight = Mathf.Exp(-dist * dist / (kernelSize * 0.5f + 0.5f));
                
                sum += result[nz, nx] * weight;
                weightSum += weight;
              }
            }
            
            smoothed[z, x] = sum / weightSum;
          }
        }
        
        result = smoothed;
      }
      
      return result;
    }

    private static float CalculateBiomeHeight(BiomeSO biome, Vector2 worldPos, Vector2 cellCenter,
      float noiseOffsetX, float noiseOffsetZ, float terrainHeight) {
      var distToCenter = Vector2.Distance(worldPos, cellCenter);
      var normalizedDist = Mathf.Clamp01(distToCenter / 100f);

      var profileHeight = biome.heightProfile.Evaluate(normalizedDist);
      var baseHeight = biome.baseHeight + profileHeight * biome.heightAmplitude;

      var noise = SampleNoise(
        worldPos.x + noiseOffsetX, 
        worldPos.y + noiseOffsetZ,
        biome.noiseFrequency,
        biome.noiseOctaves,
        biome.noisePersistence
      );

      var finalHeight = baseHeight + noise * biome.heightAmplitude * 0.5f;

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

      return total / maxValue;
    }

    /// <summary>
    /// Reset terrain to flat.
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
