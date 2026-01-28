using System.Collections.Generic;
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
    private readonly Dictionary<string, float[,]> _categoryMasks = new();
    
    // Cached prototype indices: VegetationPrototypeSO -> list of matching terrain prototype indices
    private readonly Dictionary<VegetationPrototypeSO, int[]> _prototypeIndexCache = new();
    
    // Reference to detail prototypes for cache building
    private DetailPrototype[] _detailPrototypes;
    
    // Pre-computed terrain data
    private float[,] _slopeMap;
    private int[,] _dominantSplatMap;

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
      
      // Clear caches
      _categoryMasks.Clear();
      _prototypeIndexCache.Clear();
      MaskService.ClearCache();
      
      // Create detail layers array
      var layerCount = detailPrototypes.Length;
      var detailLayers = new int[layerCount][,];
      for (int layer = 0; layer < layerCount; layer++) {
        detailLayers[layer] = new int[resolution, resolution];
      }
      
      // Pre-compute all terrain data
      ReportProgress(0.05f, "Pre-computing terrain data...");
      var heightmapRes = td.heightmapResolution;
      var heights = td.GetHeights(0, 0, heightmapRes, heightmapRes);
      
      // Pre-compute slope map
      ReportProgress(0.1f, "Computing slope map...");
      PrecomputeSlopeMap(heights, heightmapRes, terrainSize, resolution);
      
      // Pre-compute dominant splat map (for allowedTerrainLayers checks)
      ReportProgress(0.15f, "Computing splat map...");
      PrecomputeDominantSplatMap(td, resolution);
      
      // Store reference for prototype cache
      _detailPrototypes = detailPrototypes;
      
      // Pre-compute coordinate mappings
      var resMinusOne = resolution - 1;
      var heightResMinusOne = heightmapRes - 1;
      var boundsMinX = bounds.min.x;
      var boundsMinZ = bounds.min.z;
      var boundsSizeX = bounds.size.x;
      var boundsSizeZ = bounds.size.z;
      var terrainHeight = terrainSize.y;
      
      // Paint vegetation
      ReportProgress(0.2f, "Painting vegetation...");
      
      for (int y = 0; y < resolution; y++) {
        if (y % 200 == 0) {
          ReportProgress(0.2f + 0.7f * ((float)y / resolution), $"Row {y}/{resolution}");
        }
        
        for (int x = 0; x < resolution; x++) {
          // Convert detail coords to world position (inlined for speed)
          var nx = x / (float)resMinusOne;
          var ny = y / (float)resMinusOne;
          
          var worldX = boundsMinX + nx * boundsSizeX;
          var worldZ = boundsMinZ + ny * boundsSizeZ;
          
          // Get BiomeSO at this point
          var biome = biomeMap.GetBiomeDataAt(worldX, worldZ);
          if (biome?.vegetationConfig == null) continue;
          
          var vegConfig = biome.vegetationConfig;
          if (vegConfig.categories == null || vegConfig.categories.Length == 0) continue;
          
          // Get pre-computed terrain conditions
          var hx = (int)(nx * heightResMinusOne);
          var hy = (int)(ny * heightResMinusOne);
          var height = heights[hy, hx] * terrainHeight;
          var slope = _slopeMap[y, x];
          
          // Get distance from biome center (0 = center, 1 = edge)
          var biomeEdgeDistance = biomeMap.GetNormalizedDistanceToCenter(worldX, worldZ);
          
          // Process each category
          var categories = vegConfig.categories;
          for (int catIdx = 0; catIdx < categories.Length; catIdx++) {
            var category = categories[catIdx];
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
            var layers = category.layers;
            for (int layerIdx = 0; layerIdx < layers.Length; layerIdx++) {
              var vegLayer = layers[layerIdx];
              if (vegLayer?.prototype == null) continue;
              
              // Get cached prototype indices
              var protoIndices = GetCachedPrototypeIndices(vegLayer.prototype);
              if (protoIndices == null || protoIndices.Length == 0) continue;
              
              // Pick prototype index (random if multiple)
              var protoIndex = protoIndices.Length == 1 
                ? protoIndices[0] 
                : protoIndices[ctx.Random.Next(protoIndices.Length)];
              
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
              
              // Check allowed terrain layers (using pre-computed map)
              if (vegLayer.allowedTerrainLayers != null && vegLayer.allowedTerrainLayers.Length > 0) {
                var dominantLayer = _dominantSplatMap[y, x];
                if (!vegLayer.IsLayerAllowed(dominantLayer)) continue;
              }
              
              // Add some variation with weight
              var weightedDensity = finalDensity * vegLayer.weight;
              
              // Random threshold for sparse placement
              var randomThreshold = (float)ctx.Random.NextDouble();
              if (randomThreshold > weightedDensity * 2f) continue;
              
              // Convert to detail value
              var detailValue = Mathf.RoundToInt(weightedDensity * vegConfig.maxDensityPerCell);
              if (detailValue > detailLayers[protoIndex][y, x]) {
                detailLayers[protoIndex][y, x] = detailValue;
              }
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
      Cleanup();
      
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
    
    private void Cleanup() {
      _categoryMasks.Clear();
      _prototypeIndexCache.Clear();
      _slopeMap = null;
      _dominantSplatMap = null;
      _detailPrototypes = null;
    }
    
    /// <summary>
    /// Pre-compute slope map at detail resolution for fast lookup.
    /// </summary>
    private void PrecomputeSlopeMap(float[,] heights, int heightmapRes, Vector3 terrainSize, int detailRes) {
      _slopeMap = new float[detailRes, detailRes];
      
      var heightResMinusOne = heightmapRes - 1;
      var cellSize = terrainSize.x / heightmapRes;
      var heightScale = terrainSize.y / (2f * cellSize);
      
      for (int y = 0; y < detailRes; y++) {
        var ny = y / (float)(detailRes - 1);
        var hy = (int)(ny * heightResMinusOne);
        
        for (int x = 0; x < detailRes; x++) {
          var nx = x / (float)(detailRes - 1);
          var hx = (int)(nx * heightResMinusOne);
          
          // Sample neighboring heights
          var h = heights[hy, hx];
          var hL = hx > 0 ? heights[hy, hx - 1] : h;
          var hR = hx < heightResMinusOne ? heights[hy, hx + 1] : h;
          var hD = hy > 0 ? heights[hy - 1, hx] : h;
          var hU = hy < heightResMinusOne ? heights[hy + 1, hx] : h;
          
          // Calculate gradients
          var dx = (hR - hL) * heightScale;
          var dy = (hU - hD) * heightScale;
          
          // Convert to angle
          var gradient = Mathf.Sqrt(dx * dx + dy * dy);
          _slopeMap[y, x] = Mathf.Atan(gradient) * Mathf.Rad2Deg;
        }
      }
    }
    
    /// <summary>
    /// Pre-compute dominant splat layer at detail resolution.
    /// Avoids calling GetAlphamaps per-pixel during main loop.
    /// </summary>
    private void PrecomputeDominantSplatMap(TerrainData td, int detailRes) {
      _dominantSplatMap = new int[detailRes, detailRes];
      
      var alphaRes = td.alphamapResolution;
      var alphas = td.GetAlphamaps(0, 0, alphaRes, alphaRes);
      var layerCount = alphas.GetLength(2);
      
      var detailToAlpha = (float)alphaRes / detailRes;
      
      for (int y = 0; y < detailRes; y++) {
        var ay = Mathf.Clamp((int)(y * detailToAlpha), 0, alphaRes - 1);
        
        for (int x = 0; x < detailRes; x++) {
          var ax = Mathf.Clamp((int)(x * detailToAlpha), 0, alphaRes - 1);
          
          // Find dominant layer
          var maxWeight = 0f;
          var maxIndex = 0;
          for (int i = 0; i < layerCount; i++) {
            var weight = alphas[ay, ax, i];
            if (weight > maxWeight) {
              maxWeight = weight;
              maxIndex = i;
            }
          }
          
          _dominantSplatMap[y, x] = maxIndex;
        }
      }
    }
    
    /// <summary>
    /// Get cached prototype indices for a VegetationPrototypeSO.
    /// Builds cache entry on first access (lazy).
    /// </summary>
    private int[] GetCachedPrototypeIndices(VegetationPrototypeSO vegProto) {
      if (_prototypeIndexCache.TryGetValue(vegProto, out var cached)) {
        return cached;
      }
      
      // Collect all prefabs from VegetationPrototypeSO
      var prefabs = new List<GameObject>(4);
      if (vegProto.prefab != null) {
        prefabs.Add(vegProto.prefab);
      }
      if (vegProto.prefabs != null) {
        foreach (var p in vegProto.prefabs) {
          if (p != null && !prefabs.Contains(p)) {
            prefabs.Add(p);
          }
        }
      }
      
      if (prefabs.Count == 0) {
        Debug.LogWarning($"[Vegetation] VegProto '{vegProto.name}' has no prefabs!");
        _prototypeIndexCache[vegProto] = null;
        return null;
      }
      
      // Find matching indices in terrain prototypes
      var matchingIndices = new List<int>(4);
      for (int i = 0; i < _detailPrototypes.Length; i++) {
        if (prefabs.Contains(_detailPrototypes[i].prototype)) {
          matchingIndices.Add(i);
        }
      }
      
      var result = matchingIndices.Count > 0 ? matchingIndices.ToArray() : null;
      _prototypeIndexCache[vegProto] = result;
      return result;
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
  }
}
