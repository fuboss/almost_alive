using Content.Scripts.World.Biomes;
using Content.Scripts.World.Vegetation;
using Content.Scripts.World.Vegetation.Mask;
using UnityEngine;

namespace Content.Scripts.World.Generation.Pipeline.Phases {
  /// <summary>
  /// Phase 4: Paint vegetation based on biomes using category-based noise.
  /// Each category (Small/Medium/Large) has its own noise pattern for natural distribution.
  /// </summary>
  public class VegetationPhase : GenerationPhaseBase {
    
    public override string Name => "Vegetation";
    public override string Description => "Paint grass and details";

    // Cached masks per category (category index + seed offset -> mask)
    private readonly System.Collections.Generic.Dictionary<string, float[,]> _categoryMasks = new();

    protected override bool ValidateContext(GenerationContext ctx) {
      if (ctx?.BiomeMap == null) {
        Debug.LogError("[Vegetation] BiomeMap not generated");
        return false;
      }
      if (!ctx.Config.paintVegetation) {
        Debug.Log("[Vegetation] Skipped (paintVegetation disabled)");
        return false;
      }
      return true;
    }

    protected override void ExecuteInternal(GenerationContext ctx) {
      ReportProgress(0f, "Preparing vegetation...");
      
      var terrain = ctx.Terrain;
      var td = terrain.terrainData;
      var resolution = td.detailResolution;
      var biomeMap = ctx.BiomeMap;
      var bounds = ctx.Bounds;
      var terrainPos = terrain.transform.position;
      var terrainSize = td.size;
      
      var detailPrototypes = td.detailPrototypes;
      if (detailPrototypes == null || detailPrototypes.Length == 0) {
        Debug.LogWarning("[Vegetation] No detail prototypes configured on terrain");
        return;
      }
      
      // Clear mask cache
      _categoryMasks.Clear();
      MaskService.ClearCache();
      
      // Create detail layers array
      var layerCount = detailPrototypes.Length;
      var detailLayers = new int[layerCount][,];
      for (int layer = 0; layer < layerCount; layer++) {
        detailLayers[layer] = new int[resolution, resolution];
      }
      
      // Pre-sample heights and slopes for performance
      ReportProgress(0.1f, "Sampling terrain...");
      var heightmapRes = td.heightmapResolution;
      var heights = td.GetHeights(0, 0, heightmapRes, heightmapRes);
      
      // Paint vegetation
      ReportProgress(0.2f, "Painting vegetation...");
      
      for (int y = 0; y < resolution; y++) {
        if (y % 50 == 0) {
          ReportProgress(0.2f + 0.7f * ((float)y / resolution), $"Row {y}/{resolution}");
        }
        
        for (int x = 0; x < resolution; x++) {
          // Convert detail coords to world position
          var nx = x / (float)(resolution - 1);
          var ny = y / (float)(resolution - 1);
          
          var worldX = bounds.min.x + nx * bounds.size.x;
          var worldZ = bounds.min.z + ny * bounds.size.z;
          var worldPos = new Vector3(worldX, 0, worldZ);
          
          // Get BiomeSO at this point
          var biome = biomeMap.GetBiomeDataAt(worldPos);
          if (biome?.vegetationConfig == null) continue;
          
          var vegConfig = biome.vegetationConfig;
          if (vegConfig.categories == null || vegConfig.categories.Length == 0) continue;
          
          // Calculate terrain conditions (convert detail coords to heightmap coords)
          var hx = Mathf.FloorToInt(nx * (heightmapRes - 1));
          var hy = Mathf.FloorToInt(ny * (heightmapRes - 1));
          var height = heights[hy, hx] * terrainSize.y;
          var slope = CalculateSlope(heights, hx, hy, heightmapRes, terrainSize);
          
          // Get distance from biome center (0 = center, 1 = edge)
          var biomeEdgeDistance = biomeMap.GetNormalizedDistanceToCenter(new Vector2(worldX, worldZ));
          
          // Process each category
          foreach (var category in vegConfig.categories) {
            if (!category.enabled || category.layers == null) continue;
            
            // Get or create mask for this category
            var mask = GetOrCreateCategoryMask(category, biome, ctx, resolution, terrainPos, terrainSize);
            var maskValue = mask[y, x];
            
            // Skip if mask says no vegetation here
            if (maskValue <= 0.01f) continue;
            
            // Calculate category modifier
            var categoryMod = category.CalculateTerrainModifier(slope, height, biomeEdgeDistance);
            categoryMod *= vegConfig.globalDensity;
            categoryMod *= maskValue;
            
            if (categoryMod <= 0.01f) continue;
            
            // Process layers in this category
            foreach (var vegLayer in category.layers) {
              if (vegLayer?.prototype == null) continue;
              
              var protoIndex = FindPrototypeIndex(detailPrototypes, vegLayer.prototype, ctx.Random);
              if (protoIndex < 0) continue;
              
              // Calculate layer-specific noise if enabled
              var layerNoise = 0.5f;
              if (vegLayer.useLayerNoise) {
                layerNoise = Mathf.PerlinNoise(
                  worldX * vegLayer.layerNoiseScale + ctx.Seed * 0.1f,
                  worldZ * vegLayer.layerNoiseScale + ctx.Seed * 0.2f
                );
              }
              
              // Calculate final density
              var finalDensity = vegLayer.CalculateDensity(categoryMod, slope, layerNoise);
              
              // Check allowed terrain layers
              if (vegLayer.allowedTerrainLayers != null && vegLayer.allowedTerrainLayers.Length > 0) {
                var dominantLayer = GetDominantSplatLayer(td, x, y, resolution);
                if (!vegLayer.IsLayerAllowed(dominantLayer)) continue;
              }
              
              // Add some variation with weight
              var weightedDensity = finalDensity * vegLayer.weight;
              
              // Random threshold for sparse placement
              var randomThreshold = (float)ctx.Random.NextDouble();
              if (randomThreshold > weightedDensity * 2f) continue;
              
              // Convert to detail value
              var detailValue = Mathf.RoundToInt(weightedDensity * vegConfig.maxDensityPerCell);
              detailValue = Mathf.Max(detailValue, detailLayers[protoIndex][y, x]);
              
              detailLayers[protoIndex][y, x] = detailValue;
            }
          }
        }
      }
      
      ReportProgress(0.9f, "Applying detail layers...");
      
      // Apply all layers to terrain
      ctx.DetailLayers = detailLayers;
      for (int i = 0; i < layerCount; i++) {
        td.SetDetailLayer(0, 0, i, detailLayers[i]);
      }
      
      // Cleanup
      _categoryMasks.Clear();
      
      ReportProgress(1f);
      ClearProgressBar();
    }

    protected override void RollbackInternal(GenerationContext ctx) {
      if (ctx.OriginalDetailLayers != null) {
        var td = ctx.Terrain.terrainData;
        for (int i = 0; i < ctx.OriginalDetailLayers.Length; i++) {
          if (ctx.OriginalDetailLayers[i] != null) {
            td.SetDetailLayer(0, 0, i, ctx.OriginalDetailLayers[i]);
          }
        }
      }
      ctx.DetailLayers = null;
    }
    
    private float[,] GetOrCreateCategoryMask(
      VegetationCategory category, 
      BiomeSO biome,
      GenerationContext ctx, 
      int resolution, 
      Vector3 terrainPos, 
      Vector3 terrainSize) {
      
      // Build unique key for this category + biome
      var key = $"{biome.name}_{category.name}_{(int)category.size}";
      
      if (_categoryMasks.TryGetValue(key, out var cached)) {
        return cached;
      }
      
      // Create mask settings from category
      var seedOffset = (int)category.size * 1000 + biome.name.GetHashCode();
      var settings = category.noise.ToMaskSettings(seedOffset);
      
      var mask = MaskService.GetMask(
        ctx.Terrain.terrainData,
        resolution,
        terrainPos,
        terrainSize,
        ctx.Seed,
        settings
      );
      
      _categoryMasks[key] = mask;
      return mask;
    }
    
    private float CalculateSlope(float[,] heights, int x, int y, int resolution, Vector3 terrainSize) {
      // Sample neighboring heights
      var h = heights[y, x];
      var hL = x > 0 ? heights[y, x - 1] : h;
      var hR = x < resolution - 1 ? heights[y, x + 1] : h;
      var hD = y > 0 ? heights[y - 1, x] : h;
      var hU = y < resolution - 1 ? heights[y + 1, x] : h;
      
      // Calculate gradients
      var cellSize = terrainSize.x / resolution;
      var dx = (hR - hL) * terrainSize.y / (2f * cellSize);
      var dy = (hU - hD) * terrainSize.y / (2f * cellSize);
      
      // Convert to angle
      var gradient = Mathf.Sqrt(dx * dx + dy * dy);
      return Mathf.Atan(gradient) * Mathf.Rad2Deg;
    }
    
    private int GetDominantSplatLayer(TerrainData td, int x, int y, int detailResolution) {
      // Convert detail coords to alphamap coords
      var alphaRes = td.alphamapResolution;
      var ax = Mathf.FloorToInt((float)x / detailResolution * alphaRes);
      var ay = Mathf.FloorToInt((float)y / detailResolution * alphaRes);
      ax = Mathf.Clamp(ax, 0, alphaRes - 1);
      ay = Mathf.Clamp(ay, 0, alphaRes - 1);
      
      var alphas = td.GetAlphamaps(ax, ay, 1, 1);
      var layers = alphas.GetLength(2);
      
      var maxWeight = 0f;
      var maxIndex = 0;
      for (int i = 0; i < layers; i++) {
        if (alphas[0, 0, i] > maxWeight) {
          maxWeight = alphas[0, 0, i];
          maxIndex = i;
        }
      }
      
      return maxIndex;
    }
    
    private int FindPrototypeIndex(DetailPrototype[] prototypes, VegetationPrototypeSO vegProto, System.Random random = null) {
      // Collect all prefabs from VegetationPrototypeSO
      var prefabsToSearch = new System.Collections.Generic.List<GameObject>();
      
      if (vegProto.prefab != null) {
        prefabsToSearch.Add(vegProto.prefab);
      }
      if (vegProto.prefabs != null) {
        foreach (var p in vegProto.prefabs) {
          if (p != null && !prefabsToSearch.Contains(p)) {
            prefabsToSearch.Add(p);
          }
        }
      }
      
      if (prefabsToSearch.Count == 0) {
        Debug.LogWarning($"[Vegetation] VegProto '{vegProto.name}' has no prefabs!");
        return -1;
      }
      
      // Find all matching prototype indices
      var matchingIndices = new System.Collections.Generic.List<int>();
      for (int i = 0; i < prototypes.Length; i++) {
        if (prefabsToSearch.Contains(prototypes[i].prototype)) {
          matchingIndices.Add(i);
        }
      }
      
      if (matchingIndices.Count == 0) return -1;
      
      // Return random index if multiple matches, otherwise first
      if (matchingIndices.Count == 1 || random == null) {
        return matchingIndices[0];
      }
      return matchingIndices[random.Next(matchingIndices.Count)];
    }
  }
}
