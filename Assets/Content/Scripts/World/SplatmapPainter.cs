using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Paints terrain splatmap based on biome configuration.
  /// Supports 4 texture slots per biome: base, detail (noise), slope, cliff.
  /// </summary>
  public static class SplatmapPainter {
    
    /// <summary>
    /// Apply biome-based textures to terrain.
    /// </summary>
    public static void Paint(Terrain terrain, BiomeMap biomeMap, int seed = 0) {
      if (terrain == null || biomeMap == null) return;

      var terrainData = terrain.terrainData;
      var alphamapRes = terrainData.alphamapResolution;
      var layerCount = terrainData.alphamapLayers;

      if (layerCount == 0) {
        Debug.LogWarning("[SplatmapPainter] No terrain layers. Apply TerrainPalette first.");
        return;
      }

      var splatmap = new float[alphamapRes, alphamapRes, layerCount];
      var terrainPos = terrain.transform.position;
      var terrainSize = terrainData.size;

      // Noise offset for variation
      var rng = new System.Random(seed);
      var noiseOffsetX = (float)rng.NextDouble() * 10000f;
      var noiseOffsetZ = (float)rng.NextDouble() * 10000f;

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
            splatmap[z, x, 0] = 1f;
            continue;
          }

          // Clear all layers
          for (var i = 0; i < layerCount; i++) {
            splatmap[z, x, i] = 0f;
          }

          // Apply primary biome
          ApplyBiomeTextures(splatmap, z, x, layerCount, query.primaryData, 
            slope, query.primaryWeight, worldX, worldZ, noiseOffsetX, noiseOffsetZ);

          // Blend secondary biome if on border
          if (query.isBlending && query.secondaryData != null) {
            ApplyBiomeTextures(splatmap, z, x, layerCount, query.secondaryData, 
              slope, query.secondaryWeight, worldX, worldZ, noiseOffsetX, noiseOffsetZ);
          }

          // Normalize weights
          NormalizeWeights(splatmap, z, x, layerCount);
        }
      }

      terrainData.SetAlphamaps(0, 0, splatmap);
      Debug.Log($"[SplatmapPainter] Applied splatmap ({alphamapRes}x{alphamapRes}, {layerCount} layers)");
    }

    private static void ApplyBiomeTextures(float[,,] splatmap, int z, int x, int layerCount,
      BiomeSO biome, float slope, float biomeWeight, 
      float worldX, float worldZ, float noiseOffsetX, float noiseOffsetZ) {

      // Get layer indices
      var baseIdx = Mathf.Clamp(biome.GetBaseLayerIndex(), 0, layerCount - 1);
      var detailIdx = biome.GetDetailLayerIndex();
      var slopeIdx = biome.GetSlopeLayerIndex();
      var cliffIdx = biome.GetCliffLayerIndex();

      // Calculate weights for each slot
      var baseWeight = 1f;
      var detailWeight = 0f;
      var slopeWeight = 0f;
      var cliffWeight = 0f;

      // 1. Detail layer (noise blend with base)
      if (biome.detailTexture.isEnabled && detailIdx >= 0 && detailIdx < layerCount) {
        var noise = Mathf.PerlinNoise(
          (worldX + noiseOffsetX) * biome.detailTexture.noiseScale,
          (worldZ + noiseOffsetZ) * biome.detailTexture.noiseScale
        );
        detailWeight = noise * biome.detailTexture.strength;
        baseWeight -= detailWeight;
      }

      // 2. Slope layer (based on slope angle)
      if (biome.slopeTexture.isEnabled && slopeIdx >= 0 && slopeIdx < layerCount) {
        var slopeMin = biome.slopeTexture.slopeRange.x;
        var slopeMax = biome.slopeTexture.slopeRange.y;

        if (slope >= slopeMin && slope <= slopeMax) {
          // Full slope texture in the middle of range
          var midSlope = (slopeMin + slopeMax) * 0.5f;
          var halfRange = (slopeMax - slopeMin) * 0.5f;

          if (halfRange > 0.01f) {
            // Smooth blend: 0 at edges, 1 at center
            var distFromMid = Mathf.Abs(slope - midSlope);
            slopeWeight = 1f - Mathf.Clamp01(distFromMid / halfRange);
          } else {
            slopeWeight = 1f;
          }
        } else if (slope > slopeMax) {
          // Fade out above range (cliff takes over)
          slopeWeight = Mathf.Clamp01(1f - (slope - slopeMax) / 10f);
        }
      }

      // 3. Cliff layer (steep slopes)
      if (biome.cliffTexture.isEnabled && cliffIdx >= 0 && cliffIdx < layerCount) {
        if (slope > biome.cliffTexture.threshold) {
          // Blend in over 10 degree range
          cliffWeight = Mathf.Clamp01((slope - biome.cliffTexture.threshold) / 10f);
        }
      }

      // Normalize slot weights and reduce base/detail accordingly
      var terrainFeatureWeight = slopeWeight + cliffWeight;
      if (terrainFeatureWeight > 1f) {
        slopeWeight /= terrainFeatureWeight;
        cliffWeight /= terrainFeatureWeight;
        terrainFeatureWeight = 1f;
      }

      var flatWeight = 1f - terrainFeatureWeight;
      baseWeight *= flatWeight;
      detailWeight *= flatWeight;

      // Apply to splatmap with biome blend weight
      if (baseIdx >= 0) splatmap[z, x, baseIdx] += baseWeight * biomeWeight;
      if (detailIdx >= 0 && detailIdx < layerCount) splatmap[z, x, detailIdx] += detailWeight * biomeWeight;
      if (slopeIdx >= 0 && slopeIdx < layerCount) splatmap[z, x, slopeIdx] += slopeWeight * biomeWeight;
      if (cliffIdx >= 0 && cliffIdx < layerCount) splatmap[z, x, cliffIdx] += cliffWeight * biomeWeight;
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
    /// Reset splatmap to single layer.
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
