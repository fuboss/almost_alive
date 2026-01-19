using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Paints terrain splatmap (textures) based on biome configuration.
  /// </summary>
  public static class SplatmapPainter {
    /// <summary>
    /// Apply biome-based textures to terrain.
    /// </summary>
    public static void Paint(Terrain terrain, BiomeMap biomeMap) {
      if (terrain == null || biomeMap == null) return;

      var terrainData = terrain.terrainData;
      var alphamapRes = terrainData.alphamapResolution;
      var layerCount = terrainData.alphamapLayers;

      if (layerCount == 0) {
        Debug.LogWarning("[SplatmapPainter] No terrain layers configured");
        return;
      }

      var splatmap = new float[alphamapRes, alphamapRes, layerCount];
      var terrainPos = terrain.transform.position;
      var terrainSize = terrainData.size;

      for (var z = 0; z < alphamapRes; z++) {
        for (var x = 0; x < alphamapRes; x++) {
          var normalizedX = (float)x / (alphamapRes - 1);
          var normalizedZ = (float)z / (alphamapRes - 1);

          var worldX = terrainPos.x + normalizedX * terrainSize.x;
          var worldZ = terrainPos.z + normalizedZ * terrainSize.z;
          var pos2D = new Vector2(worldX, worldZ);

          // Get slope at this point
          var slope = terrainData.GetSteepness(normalizedX, normalizedZ);

          var query = biomeMap.QueryBiome(pos2D);
          if (query.primaryData == null) {
            // Default to layer 0
            splatmap[z, x, 0] = 1f;
            continue;
          }

          // Clear all layers
          for (var i = 0; i < layerCount; i++) {
            splatmap[z, x, i] = 0f;
          }

          // Apply primary biome texture
          ApplyBiomeTexture(splatmap, z, x, layerCount, query.primaryData, slope, query.primaryWeight);

          // Blend secondary biome if on border
          if (query.isBlending && query.secondaryData != null) {
            ApplyBiomeTexture(splatmap, z, x, layerCount, query.secondaryData, slope, query.secondaryWeight);
          }

          // Normalize weights
          NormalizeWeights(splatmap, z, x, layerCount);
        }
      }

      terrainData.SetAlphamaps(0, 0, splatmap);
      Debug.Log($"[SplatmapPainter] Applied splatmap ({alphamapRes}x{alphamapRes}, {layerCount} layers)");
    }

    private static void ApplyBiomeTexture(float[,,] splatmap, int z, int x, int layerCount,
      BiomeSO biome, float slope, float weight) {
      var primaryLayer = Mathf.Clamp(biome.primaryTerrainLayer, 0, layerCount - 1);
      var secondaryLayer = Mathf.Clamp(biome.secondaryTerrainLayer, 0, layerCount - 1);

      // Use secondary layer on steep slopes
      if (slope > biome.slopeThreshold && secondaryLayer != primaryLayer) {
        var slopeBlend = Mathf.Clamp01((slope - biome.slopeThreshold) / 15f);
        splatmap[z, x, primaryLayer] += weight * (1f - slopeBlend);
        splatmap[z, x, secondaryLayer] += weight * slopeBlend;
      } else {
        splatmap[z, x, primaryLayer] += weight;
      }
    }

    private static void NormalizeWeights(float[,,] splatmap, int z, int x, int layerCount) {
      var total = 0f;
      for (var i = 0; i < layerCount; i++) {
        total += splatmap[z, x, i];
      }

      if (total > 0.001f) {
        for (var i = 0; i < layerCount; i++) {
          splatmap[z, x, i] /= total;
        }
      } else {
        splatmap[z, x, 0] = 1f;
      }
    }

    /// <summary>
    /// Reset splatmap to single layer (for testing).
    /// </summary>
    public static void Clear(Terrain terrain, int defaultLayer = 0) {
      if (terrain == null) return;

      var terrainData = terrain.terrainData;
      var alphamapRes = terrainData.alphamapResolution;
      var layerCount = terrainData.alphamapLayers;

      if (layerCount == 0) return;

      var splatmap = new float[alphamapRes, alphamapRes, layerCount];
      var layer = Mathf.Clamp(defaultLayer, 0, layerCount - 1);

      for (var z = 0; z < alphamapRes; z++) {
        for (var x = 0; x < alphamapRes; x++) {
          splatmap[z, x, layer] = 1f;
        }
      }

      terrainData.SetAlphamaps(0, 0, splatmap);
      Debug.Log("[SplatmapPainter] Splatmap cleared");
    }
  }
}
