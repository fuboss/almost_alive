using System.Collections.Generic;
using Content.Scripts.World.Biomes;
using Content.Scripts.World.Vegetation.Mask;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Content.Scripts.World.Vegetation {
  /// <summary>
  /// Paints vegetation (grass, bushes) on terrain using Unity Detail system.
  /// Called during world generation after SplatmapPainter.
  /// Supports new category-based vegetation configuration.
  /// </summary>
  public static class VegetationPainter {

    public static bool CleanupAfterPaintInEditor = true;

    /// <summary>
    /// Paint vegetation on terrain based on biome map.
    /// Uses category-based noise for natural distribution patterns.
    /// </summary>
    public static void Paint(Terrain terrain, BiomeMap biomeMap, IReadOnlyList<BiomeSO> biomes, int seed) {
      if (terrain == null || biomeMap == null || biomes == null) return;

      var terrainData = terrain.terrainData;
      var detailResolution = terrainData.detailResolution;
      if (detailResolution == 0) return;

      var (prototypes, prototypeIndexMap) = CollectPrototypes(biomes);
      if (prototypes.Count == 0) return;

      terrainData.detailPrototypes = prototypes.ToArray();

      var detailLayers = InitializeDetailLayers(prototypes.Count, detailResolution);

      var terrainPos = terrain.transform.position;
      var terrainSize = terrainData.size;
      var random = new System.Random(seed + 2000);
      var totalPainted = 0;

      // Build per-biome, per-category masks
      var categoryMasks = BuildCategoryMasks(biomes, terrainData, detailResolution, terrainPos, terrainSize, seed);

      PaintDetailCells(
        terrainData, detailResolution, terrainPos, terrainSize,
        biomeMap, prototypeIndexMap, detailLayers,
        random, seed, ref totalPainted, categoryMasks);

      ApplyDetailLayers(terrainData, prototypes.Count, detailLayers);

#if UNITY_EDITOR
      if (CleanupAfterPaintInEditor) {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
      }
#endif
    }

    /// <summary>
    /// Clear all vegetation from terrain.
    /// </summary>
    public static void Clear(Terrain terrain) {
      if (terrain == null) return;

      var terrainData = terrain.terrainData;
      var resolution = terrainData.detailResolution;
      var emptyLayer = new int[resolution, resolution];

      for (var i = 0; i < terrainData.detailPrototypes.Length; i++) {
        terrainData.SetDetailLayer(0, 0, i, emptyLayer);
      }

      terrainData.detailPrototypes = new DetailPrototype[0];
    }

    /// <summary>
    /// Clear mask cache used by mask generators.
    /// </summary>
    public static void ClearMaskCache() {
      MaskService.ClearCache();
    }

    /// <summary>
    /// Build masks for each biome/category combination.
    /// </summary>
    private static Dictionary<string, float[,]> BuildCategoryMasks(
      IReadOnlyList<BiomeSO> biomes,
      TerrainData terrainData,
      int resolution,
      Vector3 terrainPos,
      Vector3 terrainSize,
      int seed) {
      
      var masks = new Dictionary<string, float[,]>();

      foreach (var biome in biomes) {
        if (biome?.vegetationConfig?.categories == null) continue;

        foreach (var category in biome.vegetationConfig.categories) {
          if (category == null || !category.enabled) continue;

          var key = $"{biome.name}_{category.name}_{(int)category.size}";
          if (masks.ContainsKey(key)) continue;

          var seedOffset = (int)category.size * 1000 + biome.name.GetHashCode();
          var settings = category.noise.ToMaskSettings(seedOffset);
          var mask = MaskService.GetMask(terrainData, resolution, terrainPos, terrainSize, seed, settings);
          masks[key] = mask;
        }
      }

      return masks;
    }

    /// <summary>
    /// Collect all unique vegetation prototypes from biomes.
    /// </summary>
    private static (List<DetailPrototype>, Dictionary<VegetationPrototypeSO, List<int>>) CollectPrototypes(
      IReadOnlyList<BiomeSO> biomes) {
      var prototypes = new List<DetailPrototype>();
      var indexMap = new Dictionary<VegetationPrototypeSO, List<int>>();

      foreach (var biome in biomes) {
        if (biome?.vegetationConfig?.categories == null) continue;

        foreach (var category in biome.vegetationConfig.categories) {
          if (category?.layers == null) continue;

          foreach (var layer in category.layers) {
            if (layer?.prototype == null) continue;
            if (indexMap.ContainsKey(layer.prototype) && indexMap[layer.prototype] != null) continue;

            foreach (var prototype in layer.prototype.ToDetailPrototypes()) {
              if (prototype == null) continue;

              if (!indexMap.TryGetValue(layer.prototype, out var list)) {
                list = new List<int>();
                indexMap[layer.prototype] = list;
              }

              list.Add(prototypes.Count);
              prototypes.Add(prototype);
            }
          }
        }
      }

      return (prototypes, indexMap);
    }

    private static int[][,] InitializeDetailLayers(int prototypeCount, int resolution) {
      var detailLayers = new int[prototypeCount][,];
      for (var i = 0; i < prototypeCount; i++) detailLayers[i] = new int[resolution, resolution];
      return detailLayers;
    }

    private static void PaintDetailCells(
      TerrainData terrainData,
      int detailResolution,
      Vector3 terrainPos,
      Vector3 terrainSize,
      BiomeMap biomeMap,
      Dictionary<VegetationPrototypeSO, List<int>> prototypeIndexMap,
      int[][,] detailLayers,
      System.Random random,
      int seed,
      ref int totalPainted,
      Dictionary<string, float[,]> categoryMasks) {
      
      var alphamapW = terrainData.alphamapWidth;
      var alphamapH = terrainData.alphamapHeight;
      var alphamaps = terrainData.GetAlphamaps(0, 0, alphamapW, alphamapH);

      var hmW = terrainData.heightmapResolution;
      var heightmap = terrainData.GetHeights(0, 0, hmW, hmW);

      var heightScale = terrainData.size.y;
      var sampleSpacingX = terrainData.size.x / (hmW - 1);
      var sampleSpacingZ = terrainData.size.z / (hmW - 1);

      var rowsPerBatch = Mathf.Max(8, detailResolution / 8);

      try {
        for (var startRow = 0; startRow < detailResolution; startRow += rowsPerBatch) {
          var endRow = Mathf.Min(startRow + rowsPerBatch, detailResolution);

#if UNITY_EDITOR
          if (!Application.isPlaying) {
            var progress = (float)startRow / detailResolution;
            if (EditorUtility.DisplayCancelableProgressBar("Painting Vegetation", $"Rows {startRow}..{endRow} / {detailResolution}", progress)) {
              break;
            }
          }
#endif

          for (var z = startRow; z < endRow; z++) {
            for (var x = 0; x < detailResolution; x++) {
              var normalizedX = (float)x / detailResolution;
              var normalizedZ = (float)z / detailResolution;

              var wx = terrainPos.x + normalizedX * terrainSize.x;
              var wz = terrainPos.z + normalizedZ * terrainSize.z;
              var worldPos = new Vector3(wx, 0, wz);

              // Dominant texture
              var ax = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (alphamapW - 1)), 0, alphamapW - 1);
              var az = Mathf.Clamp(Mathf.RoundToInt(normalizedZ * (alphamapH - 1)), 0, alphamapH - 1);
              var dominantLayer = GetDominantLayer(alphamaps, ax, az);

              // Height
              var heightMeters = SampleHeight(heightmap, normalizedX, normalizedZ, hmW, heightScale);

              // Slope
              var slope = CalculateSlope(heightmap, normalizedX, normalizedZ, hmW, heightScale, sampleSpacingX, sampleSpacingZ);

              // Biome edge distance
              var biomeEdgeDistance = biomeMap.GetNormalizedDistanceToCenter(new Vector2(wx, wz));

              ProcessCell(
                worldPos, biomeMap, prototypeIndexMap, detailLayers,
                random, seed, ref totalPainted, z, x,
                categoryMasks, heightMeters, slope, dominantLayer, biomeEdgeDistance);
            }
          }
        }
      } finally {
#if UNITY_EDITOR
        EditorUtility.ClearProgressBar();
#endif
      }
    }

    private static void ProcessCell(
      Vector3 worldPos,
      BiomeMap biomeMap,
      Dictionary<VegetationPrototypeSO, List<int>> prototypeIndexMap,
      int[][,] detailLayers,
      System.Random random,
      int seed,
      ref int totalPainted,
      int layerZ,
      int layerX,
      Dictionary<string, float[,]> categoryMasks,
      float height,
      float slope,
      int dominantLayer,
      float biomeEdgeDistance) {

      var biomeData = biomeMap.GetBiomeDataAt(worldPos);
      if (biomeData?.vegetationConfig?.categories == null) return;

      var vegConfig = biomeData.vegetationConfig;

      foreach (var category in vegConfig.categories) {
        if (category == null || !category.enabled || category.layers == null) continue;

        // Get category mask
        var maskKey = $"{biomeData.name}_{category.name}_{(int)category.size}";
        var maskValue = 1f;
        if (categoryMasks.TryGetValue(maskKey, out var mask)) {
          maskValue = mask[layerZ, layerX];
        }

        if (maskValue <= 0.01f) continue;

        // Category terrain modifier
        var categoryMod = category.CalculateTerrainModifier(slope, height, biomeEdgeDistance);
        categoryMod *= vegConfig.globalDensity;
        categoryMod *= maskValue;

        if (categoryMod <= 0.01f) continue;

        foreach (var layer in category.layers) {
          if (layer?.prototype == null) continue;
          if (!layer.IsLayerAllowed(dominantLayer)) continue;

          if (!prototypeIndexMap.TryGetValue(layer.prototype, out var indices)) continue;
          if (indices == null || indices.Count == 0) continue;

          var chosenIdx = indices.Count > 1 ? indices[random.Next(0, indices.Count)] : indices[0];

          // Layer noise
          var layerNoise = 0.5f;
          if (layer.useLayerNoise) {
            layerNoise = Mathf.PerlinNoise(
              worldPos.x * layer.layerNoiseScale + seed * 0.1f,
              worldPos.z * layer.layerNoiseScale + seed * 0.2f
            );
          }

          var density = layer.CalculateDensity(categoryMod, slope, layerNoise);

          // Random threshold
          var randomThreshold = (float)random.NextDouble();
          if (randomThreshold > density * 2f) continue;

          var maxDensity = vegConfig.maxDensityPerCell;
          var intDensity = Mathf.RoundToInt(density * maxDensity);
          intDensity = Mathf.Clamp(intDensity, 0, maxDensity);

          if (intDensity > 0) {
            var prev = detailLayers[chosenIdx][layerZ, layerX];
            var sum = Mathf.Clamp(prev + intDensity, 0, maxDensity);
            detailLayers[chosenIdx][layerZ, layerX] = sum;
            totalPainted++;
          }
        }
      }
    }

    private static int GetDominantLayer(float[,,] alphamaps, int ax, int az) {
      var layers = alphamaps.GetLength(2);
      var maxAlpha = 0f;
      var dominant = 0;
      for (var i = 0; i < layers; i++) {
        var a = alphamaps[az, ax, i];
        if (a > maxAlpha) {
          maxAlpha = a;
          dominant = i;
        }
      }
      return dominant;
    }

    private static float SampleHeight(float[,] heightmap, float nx, float nz, int hmW, float heightScale) {
      var hxF = nx * (hmW - 1);
      var hzF = nz * (hmW - 1);
      var hx0 = Mathf.Clamp((int)Mathf.Floor(hxF), 0, hmW - 1);
      var hz0 = Mathf.Clamp((int)Mathf.Floor(hzF), 0, hmW - 1);
      var hx1 = Mathf.Clamp(hx0 + 1, 0, hmW - 1);
      var hz1 = Mathf.Clamp(hz0 + 1, 0, hmW - 1);
      var sx = hxF - hx0;
      var sz = hzF - hz0;

      var h00 = heightmap[hz0, hx0];
      var h10 = heightmap[hz0, hx1];
      var h01 = heightmap[hz1, hx0];
      var h11 = heightmap[hz1, hx1];

      var h0 = Mathf.Lerp(h00, h10, sx);
      var h1 = Mathf.Lerp(h01, h11, sx);
      return Mathf.Lerp(h0, h1, sz) * heightScale;
    }

    private static float CalculateSlope(float[,] heightmap, float nx, float nz, int hmW, float heightScale, float spacingX, float spacingZ) {
      var hx = Mathf.Clamp((int)(nx * (hmW - 1)), 1, hmW - 2);
      var hz = Mathf.Clamp((int)(nz * (hmW - 1)), 1, hmW - 2);

      var hxm = heightmap[hz, hx - 1] * heightScale;
      var hxp = heightmap[hz, hx + 1] * heightScale;
      var hzm = heightmap[hz - 1, hx] * heightScale;
      var hzp = heightmap[hz + 1, hx] * heightScale;

      var dhdx = (hxp - hxm) / (2f * spacingX);
      var dhdz = (hzp - hzm) / (2f * spacingZ);
      return Mathf.Atan(Mathf.Sqrt(dhdx * dhdx + dhdz * dhdz)) * Mathf.Rad2Deg;
    }

    private static void ApplyDetailLayers(TerrainData terrainData, int prototypeCount, int[][,] detailLayers) {
      for (var i = 0; i < prototypeCount; i++) {
        terrainData.SetDetailLayer(0, 0, i, detailLayers[i]);
      }
    }
  }
}
