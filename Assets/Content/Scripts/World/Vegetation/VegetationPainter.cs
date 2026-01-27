using System.Collections.Generic;
using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World.Vegetation {
  /// <summary>
  /// Paints vegetation (grass, bushes) on terrain using Unity Detail system.
  /// Called during world generation after SplatmapPainter.
  /// </summary>
  public static class VegetationPainter {
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

      PaintDetailCells(
        terrainData, detailResolution, terrainPos, terrainSize,
        biomeMap, prototypeIndexMap, detailLayers,
        random, seed, ref totalPainted, brushAlpha: 1f);

      ApplyDetailLayers(terrainData, prototypes.Count, detailLayers);
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
    /// Get dominant terrain texture layer at position.
    /// </summary>
    private static int GetDominantTextureLayer(TerrainData terrainData, float normalizedX, float normalizedZ) {
      var alphamapX = Mathf.RoundToInt(normalizedX * (terrainData.alphamapWidth - 1));
      var alphamapZ = Mathf.RoundToInt(normalizedZ * (terrainData.alphamapHeight - 1));
      var alphas = terrainData.GetAlphamaps(alphamapX, alphamapZ, 1, 1);

      var maxAlpha = 0f;
      var dominantLayer = 0;
      for (var i = 0; i < alphas.GetLength(2); i++) {
        if (alphas[0, 0, i] > maxAlpha) {
          maxAlpha = alphas[0, 0, i];
          dominantLayer = i;
        }
      }

      return dominantLayer;
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

      // Paint with brush: pass brush alpha sample into processing via paint loop
      PaintDetailCells(
        terrainData, detailResolution, terrainPos, terrainSize,
        biomeMap, prototypeIndexMap, detailLayers,
        random, seed, ref totalPainted,
        brushCenterWorld: brushCenterWorld, brushSizeWorld: brushSizeWorld, halfBrush: halfBrush, brush: brush);

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
      float brushAlpha = 1f
    ) {
      for (var z = 0; z < detailResolution; z++) {
        for (var x = 0; x < detailResolution; x++) {
          var normalizedX = (float)x / detailResolution;
          var normalizedZ = (float)z / detailResolution;
          var worldPos = new Vector3(
            terrainPos.x + normalizedX * terrainSize.x,
            0,
            terrainPos.z + normalizedZ * terrainSize.z
          );

          float effectiveBrushAlpha = 1f;
          if (brush != null && brushCenterWorld.HasValue) {
            // brush area test
            var brushCenter = brushCenterWorld.Value;
            var dx = worldPos.x - brushCenter.x;
            var dz = worldPos.z - brushCenter.z;
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

            // combine with provided brushAlpha if any
            effectiveBrushAlpha *= brushAlpha;
          }

          ProcessCell(
            terrainData, normalizedX, normalizedZ, worldPos,
            biomeMap, prototypeIndexMap, detailLayers,
            random, seed, effectiveBrushAlpha, ref totalPainted, z, x);
        }
      }
    }

    private static void ProcessCell(
      TerrainData terrainData,
      float normalizedX,
      float normalizedZ,
      Vector3 worldPos,
      BiomeMap biomeMap,
      Dictionary<VegetationPrototypeSO, List<int>> prototypeIndexMap,
      int[][,] detailLayers,
      System.Random random,
      int seed,
      float brushAlpha,
      ref int totalPainted,
      int layerZ,
      int layerX) {

      var biomeData = biomeMap.GetBiomeDataAt(worldPos);
      if (biomeData?.vegetationConfig?.layers == null) return;

      var height = terrainData == null ? 0f : Terrain.activeTerrain.SampleHeight(worldPos);
      var slope = terrainData.GetSteepness(normalizedX, normalizedZ);
      var dominantLayer = GetDominantTextureLayer(terrainData, normalizedX, normalizedZ);

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