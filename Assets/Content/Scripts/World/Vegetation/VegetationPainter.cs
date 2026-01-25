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
      
      if (detailResolution == 0) {
        Debug.LogWarning("[VegetationPainter] Detail resolution is 0! Set it in Terrain Settings.");
        return;
      }
      
      // Collect all unique prototypes from all biomes
      var (prototypes, prototypeIndexMap) = CollectPrototypes(biomes);
      if (prototypes.Count == 0) {
        Debug.LogWarning("[VegetationPainter] No vegetation prototypes configured in any biome");
        return;
      }
      
      Debug.Log($"[VegetationPainter] Painting {prototypes.Count} prototypes at {detailResolution}x{detailResolution}");
      
      // Set prototypes on terrain
      terrainData.detailPrototypes = prototypes.ToArray();
      
      // Create detail layers (one per prototype)
      var detailLayers = new int[prototypes.Count][,];
      for (var i = 0; i < prototypes.Count; i++) {
        detailLayers[i] = new int[detailResolution, detailResolution];
      }
      
      // Paint each cell
      var terrainPos = terrain.transform.position;
      var terrainSize = terrainData.size;
      var random = new System.Random(seed + 2000);
      var totalPainted = 0;
      
      for (var z = 0; z < detailResolution; z++) {
        for (var x = 0; x < detailResolution; x++) {
          // World position of this detail cell
          var normalizedX = (float)x / detailResolution;
          var normalizedZ = (float)z / detailResolution;
          var worldPos = new Vector3(
            terrainPos.x + normalizedX * terrainSize.x,
            0,
            terrainPos.z + normalizedZ * terrainSize.z
          );
          
          // Get biome at this position
          var biomeData = biomeMap.GetBiomeDataAt(worldPos);
          if (biomeData?.vegetationConfig?.layers == null) continue;
          if (biomeData.vegetationConfig.layers.Length == 0) continue;
          
          // Get terrain info
          var height = terrain.SampleHeight(worldPos);
          var slope = terrainData.GetSteepness(normalizedX, normalizedZ);
          var dominantLayer = GetDominantTextureLayer(terrainData, normalizedX, normalizedZ);
          
          // Process each vegetation layer in this biome
          foreach (var layer in biomeData.vegetationConfig.layers) {
            if (layer?.prototype == null) continue;
            if (!layer.IsLayerAllowed(dominantLayer)) continue;
            
            // Get prototype index
            if (!prototypeIndexMap.TryGetValue(layer.prototype, out var prototypeIndex)) continue;
            
            // Calculate noise
            var noiseValue = 0.5f;
            if (layer.useNoise) {
              noiseValue = Mathf.PerlinNoise(
                worldPos.x * layer.noiseScale + seed * 0.1f,
                worldPos.z * layer.noiseScale + seed * 0.1f
              );
            }
            
            // Calculate final density
            var density = layer.CalculateDensity(slope, height, noiseValue);
            density *= biomeData.vegetationConfig.densityMultiplier;
            
            // Add some randomization
            density *= 0.8f + (float)random.NextDouble() * 0.4f;
            
            // Convert to density range
            var maxDensity = biomeData.vegetationConfig.maxDensityPerCell;
            var intDensity = Mathf.RoundToInt(density * maxDensity);
            intDensity = Mathf.Clamp(intDensity, 0, maxDensity);
            if (intDensity > 0) {
              detailLayers[prototypeIndex][z, x] = intDensity;
              totalPainted++;
            }
          }
        }
      }
      
      // Apply all detail layers to terrain
      for (var i = 0; i < prototypes.Count; i++) {
        terrainData.SetDetailLayer(0, 0, i, detailLayers[i]);
      }
      
      Debug.Log($"[VegetationPainter] ✓ Painted {totalPainted} cells");
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
    private static (List<DetailPrototype>, Dictionary<VegetationPrototypeSO, int>) CollectPrototypes(
      IReadOnlyList<BiomeSO> biomes) {
      
      var prototypes = new List<DetailPrototype>();
      var indexMap = new Dictionary<VegetationPrototypeSO, int>();
      
      foreach (var biome in biomes) {
        if (biome?.vegetationConfig?.layers == null) continue;
        
        foreach (var layer in biome.vegetationConfig.layers) {
          if (layer?.prototype == null) continue;
          if (indexMap.ContainsKey(layer.prototype)) continue;
          var prototype = layer.prototype.ToDetailPrototype(layer.coverage);
          if (prototype == null) {
            Debug.LogError($"{layer.prototype.name} has invalid prefab. Cannot create DetailPrototype.");
            continue;
          }
          indexMap[layer.prototype] = prototypes.Count;
          prototypes.Add(prototype);
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
  }
}
