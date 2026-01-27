using System.Collections.Generic;
using Content.Scripts.World.Biomes;
using UnityEngine;
using Content.Scripts.World.Vegetation.Mask;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Content.Scripts.World.Vegetation {
  /// <summary>
  /// Paints vegetation (grass, bushes) on terrain using Unity Detail system.
  /// Called during world generation after SplatmapPainter.
  /// </summary>
  public static class VegetationPainter {

    // Enable aggressive cleanup in Editor after painting to reduce post-generation editor lag.
    // Set to true to call GC.Collect() and Resources.UnloadUnusedAssets() after painting (Editor only).
    public static bool CleanupAfterPaintInEditor = true;

    // Global mask settings (can be tweaked from code / later exposed in inspector)
    public static MaskSettings VegetationMaskSettings = new MaskSettings();

    /// <summary>
    /// Paint vegetation on terrain based on biome map.
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

      // generate default/global mask once per paint call (may be cached by MaskService)
      var defaultMask = MaskService.GetMask(terrainData, detailResolution, terrainPos, terrainSize, seed, VegetationMaskSettings);

      // Build per-biome masks using each biome's BiomeVegetationConfig — stored in a dictionary.
      var biomeMasks = new Dictionary<BiomeSO, float[,]>();
      foreach (var b in biomes) {
        if (b == null || b.vegetationConfig == null) continue;

        var cfg = b.vegetationConfig;
        var s = new MaskSettings();
        s.mode = cfg.maskMode;
        s.scale = cfg.maskScale;
        s.fbmOctaves = cfg.maskOctaves;
        s.fbmPersistence = cfg.maskPersistence;
        s.fbmLacunarity = 2f; // default lacunarity
        s.threshold = cfg.maskThreshold;
        s.blend = cfg.maskBlend;
        s.useStochasticCulling = cfg.maskUseStochastic;
        s.stochasticBlend = cfg.maskStochasticBlend;
        s.cacheEnabled = cfg.maskCacheEnabled;
        // Note: no per-biome seedOffset provided by config — rely on the global seed parameter to MaskService.

        var mask = MaskService.GetMask(terrainData, detailResolution, terrainPos, terrainSize, seed, s);
        biomeMasks[b] = mask;
      }

      PaintDetailCells(
        terrainData, detailResolution, terrainPos, terrainSize,
        biomeMap, prototypeIndexMap, detailLayers,
        random, seed, ref totalPainted, defaultMask: defaultMask, biomeMasks: biomeMasks, settings: VegetationMaskSettings, brushAlpha: 1f);

      ApplyDetailLayers(terrainData, prototypes.Count, detailLayers);

#if UNITY_EDITOR
      if (CleanupAfterPaintInEditor) {
        // Request unload and GC to reduce editor lag after heavy generation.
        // This is Editor-only and optional; it can be disabled by setting CleanupAfterPaintInEditor = false.
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
    /// Collect all unique vegetation prototypes from biomes.
    /// </summary>
    private static (List<DetailPrototype>, Dictionary<VegetationPrototypeSO, List<int>>) CollectPrototypes(
      IReadOnlyList<BiomeSO> biomes) {
      var prototypes = new List<DetailPrototype>();
      var indexMap = new Dictionary<VegetationPrototypeSO, List<int>>();

      foreach (var biome in biomes) {
        if (biome?.vegetationConfig?.layers == null) continue;

        foreach (var layer in biome.vegetationConfig.layers) {
          if (layer?.prototype == null) continue;
          if (indexMap.ContainsKey(layer.prototype) && indexMap[layer.prototype] != null) continue;
          foreach (var prototype in layer.prototype.ToDetailPrototypes(layer.coverage)) {
            if (prototype == null) {
              // лог убран по просьбе — молча пропускаем некорректный прототип
              continue;
            }

            if (!indexMap.TryGetValue(layer.prototype, out var list)) {
              list = new List<int>();
              indexMap[layer.prototype] = list;
            }

            list.Add(prototypes.Count);
            prototypes.Add(prototype);
          }
        }
      }

      return (prototypes, indexMap);
    }

    /// <summary>
    /// Paint vegetation within an area defined by a brush texture.
    /// brush: texture where alpha (preferred) or darkness controls density (0..1).
    /// brushCenterWorld: world-space center of the brush.
    /// brushSizeWorld: diameter (world units) covered by the brush.
    /// </summary>
    public static void Paint(Terrain terrain, BiomeMap biomeMap, IReadOnlyList<BiomeSO> biomes,
      Texture2D brush, Vector3 brushCenterWorld, float brushSizeWorld, int seed) {
      if (terrain == null || biomeMap == null || biomes == null || brush == null) return;

      var terrainData = terrain.terrainData;
      var detailResolution = terrainData.detailResolution;
      if (detailResolution == 0) return;

      var (prototypes, prototypeIndexMap) = CollectPrototypes(biomes);
      if (prototypes.Count == 0) return;

      terrainData.detailPrototypes = prototypes.ToArray();

      var detailLayers = InitializeDetailLayers(prototypes.Count, detailResolution);

      var terrainPos = terrain.transform.position;
      var terrainSize = terrainData.size;
      var random = new System.Random(seed + 3000);
      var halfBrush = brushSizeWorld * 0.5f;
      var totalPainted = 0;

      // generate default/global mask once per paint call (may be cached by MaskService)
      var defaultMask = MaskService.GetMask(terrainData, detailResolution, terrainPos, terrainSize, seed, VegetationMaskSettings);

      // Build per-biome masks using each biome's BiomeVegetationConfig — stored in a dictionary.
      var biomeMasks = new Dictionary<BiomeSO, float[,]>();
      foreach (var b in biomes) {
        if (b == null || b.vegetationConfig == null) continue;

        var cfg = b.vegetationConfig;
        var s = new MaskSettings();
        s.mode = cfg.maskMode;
        s.scale = cfg.maskScale;
        s.fbmOctaves = cfg.maskOctaves;
        s.fbmPersistence = cfg.maskPersistence;
        s.fbmLacunarity = 2f;
        s.threshold = cfg.maskThreshold;
        s.blend = cfg.maskBlend;
        s.useStochasticCulling = cfg.maskUseStochastic;
        s.stochasticBlend = cfg.maskStochasticBlend;
        s.cacheEnabled = cfg.maskCacheEnabled;

        var mask = MaskService.GetMask(terrainData, detailResolution, terrainPos, terrainSize, seed, s);
        biomeMasks[b] = mask;
      }

      // Paint with brush: pass brush alpha sample into processing via paint loop
      PaintDetailCells(
        terrainData, detailResolution, terrainPos, terrainSize,
        biomeMap, prototypeIndexMap, detailLayers,
        random, seed, ref totalPainted,
        brushCenterWorld: brushCenterWorld, brushSizeWorld: brushSizeWorld, halfBrush: halfBrush, brush: brush,
        defaultMask: defaultMask, biomeMasks: biomeMasks, settings: VegetationMaskSettings);

      ApplyDetailLayers(terrainData, prototypes.Count, detailLayers);
    }

    // ---------------------- Helpers ----------------------
    private static int[][,] InitializeDetailLayers(int prototypeCount, int resolution) {
      var detailLayers = new int[prototypeCount][,];
      for (var i = 0; i < prototypeCount; i++) detailLayers[i] = new int[resolution, resolution];
      return detailLayers;
    }

    // main paint loop (supports brush when brush != null)
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
      Vector3? brushCenterWorld = null,
      float brushSizeWorld = 0f,
      float halfBrush = 0f,
      Texture2D brush = null,
      float brushAlpha = 1f,
      float[,] defaultMask = null,
      Dictionary<BiomeSO, float[,]> biomeMasks = null,
      MaskSettings settings = null
    ) {
      settings = settings ?? VegetationMaskSettings;

      // Prefetch expensive data once
      var alphamapW = terrainData.alphamapWidth;
      var alphamapH = terrainData.alphamapHeight;
      var alphamaps = terrainData.GetAlphamaps(0, 0, alphamapW, alphamapH);

      var hmW = terrainData.heightmapResolution;
      var hmH = terrainData.heightmapResolution;
      var heightmap = terrainData.GetHeights(0, 0, hmW, hmH);

      var heightScale = terrainData.size.y;
      var sampleSpacingX = terrainData.size.x / (hmW - 1);
      var sampleSpacingZ = terrainData.size.z / (hmH - 1);

      var rowsPerBatch = Mathf.Max(8, detailResolution / 8);
      // cancelled variable removed - DisplayCancelableProgressBar will break out directly when requested

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

              // world pos
              var wx = terrainPos.x + normalizedX * terrainSize.x;
              var wz = terrainPos.z + normalizedZ * terrainSize.z;
              var worldPos = new Vector3(wx, 0, wz);

              // sample brush if present
              float effectiveBrushAlpha = 1f;
              if (brush != null && brushCenterWorld.HasValue) {
                var brushCenter = brushCenterWorld.Value;
                var dx = wx - brushCenter.x;
                var dz = wz - brushCenter.z;
                var distSq = dx * dx + dz * dz;
                if (distSq > halfBrush * halfBrush) continue;

                var u = 0.5f + dx / brushSizeWorld;
                var v = 0.5f + dz / brushSizeWorld;
                u = Mathf.Clamp01(u);
                v = Mathf.Clamp01(v);
                var col = brush.GetPixelBilinear(u, v);
                effectiveBrushAlpha = col.a;
                if (effectiveBrushAlpha <= 0f) {
                  var lum = (col.r + col.g + col.b) / 3f;
                  effectiveBrushAlpha = 1f - lum;
                }
                if (effectiveBrushAlpha <= 0f) continue;
                effectiveBrushAlpha *= brushAlpha;
              }

              // dominant texture from cached alphamaps
              var ax = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (alphamapW - 1)), 0, alphamapW - 1);
              var az = Mathf.Clamp(Mathf.RoundToInt(normalizedZ * (alphamapH - 1)), 0, alphamapH - 1);
              var dominantLayer = 0;
              var maxAlpha = 0f;
              var alphaLayers = alphamaps.GetLength(2);
              for (var li = 0; li < alphaLayers; li++) {
                var a = alphamaps[az, ax, li];
                if (a > maxAlpha) { maxAlpha = a; dominantLayer = li; }
              }

              // height: bilinear sample from heightmap (normalized 0..1) -> meters
              var hxF = normalizedX * (hmW - 1);
              var hzF = normalizedZ * (hmH - 1);
              var hx0 = Mathf.Clamp((int)Mathf.Floor(hxF), 0, hmW - 1);
              var hz0 = Mathf.Clamp((int)Mathf.Floor(hzF), 0, hmH - 1);
              var hx1 = Mathf.Clamp(hx0 + 1, 0, hmW - 1);
              var hz1 = Mathf.Clamp(hz0 + 1, 0, hmH - 1);
              var sx = hxF - hx0;
              var sz = hzF - hz0;

              var h00 = heightmap[hz0, hx0];
              var h10 = heightmap[hz0, hx1];
              var h01 = heightmap[hz1, hx0];
              var h11 = heightmap[hz1, hx1];

              var h0 = Mathf.Lerp(h00, h10, sx);
              var h1 = Mathf.Lerp(h01, h11, sx);
              var heightNorm = Mathf.Lerp(h0, h1, sz);
              var heightMeters = heightNorm * heightScale;

              // slope: central differences in heightmap (meters per meter) -> degrees
              var hxm = Mathf.Clamp((int)(hx0 - 1), 0, hmW - 1);
              var hxp = Mathf.Clamp((int)(hx1 + 1), 0, hmW - 1);
              var hzm = Mathf.Clamp((int)(hz0 - 1), 0, hmH - 1);
              var hzp = Mathf.Clamp((int)(hz1 + 1), 0, hmH - 1);

              var hxmVal = heightmap[hz0, hxm] * heightScale;
              var hxpVal = heightmap[hz0, hxp] * heightScale;
              var hzmVal = heightmap[hzm, hx0] * heightScale;
              var hzpVal = heightmap[hzp, hx0] * heightScale;

              var dhdx = (hxpVal - hxmVal) / ( (hxp - hxm) * sampleSpacingX + 1e-6f );
              var dhdz = (hzpVal - hzmVal) / ( (hzp - hzm) * sampleSpacingZ + 1e-6f );
              var slope = Mathf.Atan(Mathf.Sqrt(dhdx * dhdx + dhdz * dhdz)) * Mathf.Rad2Deg;

              // process cell with precomputed values

              ProcessCell(
                worldPos,
                biomeMap, prototypeIndexMap, detailLayers,
                random, seed, effectiveBrushAlpha, ref totalPainted, z, x,
                defaultMask, biomeMasks, heightMeters, slope, dominantLayer
              );
            }
          }
        }
      } finally {
#if UNITY_EDITOR
        EditorUtility.ClearProgressBar();
#endif
      }

      // large temporary arrays (alphamaps/heightmap) will be eligible for GC after this method returns

    }

    private static void ProcessCell(
      Vector3 worldPos,
      BiomeMap biomeMap,
      Dictionary<VegetationPrototypeSO, List<int>> prototypeIndexMap,
      int[][,] detailLayers,
      System.Random random,
      int seed,
      float brushAlpha,
      ref int totalPainted,
      int layerZ,
      int layerX,
      float[,] defaultMask,
      Dictionary<BiomeSO, float[,]> biomeMasks,
      float height,
      float slope,
      int dominantLayer) {

      var biomeData = biomeMap.GetBiomeDataAt(worldPos);
      if (biomeData?.vegetationConfig?.layers == null) return;


      foreach (var layer in biomeData.vegetationConfig.layers) {
        if (layer?.prototype == null) continue;
        if (!layer.IsLayerAllowed(dominantLayer)) continue;

        if (!prototypeIndexMap.TryGetValue(layer.prototype, out var indices)) continue;
        if (indices == null || indices.Count == 0) continue;

        var chosenIdx = indices.Count > 1 ? indices[random.Next(0, indices.Count)] : indices[0];

        var noiseValue = ComputeNoiseValue(layer, worldPos, seed);

        var density = layer.CalculateDensity(slope, height, noiseValue);

        density *= brushAlpha;
        density *= layer.weight;
        density *= biomeData.vegetationConfig.densityMultiplier;

        // determine mask value: prefer biome-specific mask if present, otherwise fallback to default mask
        var maskValue = 1f;
        if (biomeMasks != null && biomeData != null && biomeMasks.TryGetValue(biomeData, out var bm) && bm != null) {
          maskValue = bm[layerZ, layerX];
        } else if (defaultMask != null) {
          maskValue = defaultMask[layerZ, layerX];
        }

        // apply mask multiplicatively to modify density distribution
        density *= maskValue;

        // Add some randomization
        density *= 0.8f + (float)random.NextDouble() * 0.4f;

        var maxDensity = biomeData.vegetationConfig.maxDensityPerCell;
        var intDensity = ComputeIntDensity(density, maxDensity);
        if (intDensity > 0) {
          var prev = detailLayers[chosenIdx][layerZ, layerX];
          var sum = Mathf.Clamp(prev + intDensity, 0, maxDensity);
          detailLayers[chosenIdx][layerZ, layerX] = sum;
          totalPainted++;
        }
      }
    }

    private static float ComputeNoiseValue(VegetationLayerConfig layer, Vector3 worldPos, int seed) {
      if (layer == null) return 0.5f;
      if (!layer.useNoise) return 0.5f;
      return Mathf.PerlinNoise(
        worldPos.x * layer.noiseScale + seed * 0.1f,
        worldPos.z * layer.noiseScale + seed * 0.1f
      );
    }

    private static int ComputeIntDensity(float density, int maxDensity) {
      var intDensity = Mathf.RoundToInt(density * maxDensity);
      return Mathf.Clamp(intDensity, 0, maxDensity);
    }

    private static void ApplyDetailLayers(TerrainData terrainData, int prototypeCount, int[][,] detailLayers) {
      for (var i = 0; i < prototypeCount; i++) {
        terrainData.SetDetailLayer(0, 0, i, detailLayers[i]);
      }
    }
  }
}

