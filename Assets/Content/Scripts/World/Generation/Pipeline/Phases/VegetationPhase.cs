using UnityEngine;

namespace Content.Scripts.World.Generation.Pipeline.Phases {
  /// <summary>
  /// Phase 4: Paint grass and small vegetation based on biomes.
  /// Uses terrain detail system for efficient grass rendering.
  /// </summary>
  public class VegetationPhase : GenerationPhaseBase {
    
    public override string Name => "Vegetation";
    public override string Description => "Paint grass and details";

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
      ReportProgress(0f, "Painting vegetation...");
      
      var terrain = ctx.Terrain;
      var td = terrain.terrainData;
      var resolution = td.detailResolution;
      var biomeMap = ctx.BiomeMap;
      var bounds = ctx.Bounds;
      
      var detailPrototypes = td.detailPrototypes;
      if (detailPrototypes == null || detailPrototypes.Length == 0) {
        Debug.LogWarning("[Vegetation] No detail prototypes configured");
        return;
      }
      
      // Create detail layers array: int[][,] - array of 2D arrays
      var layerCount = detailPrototypes.Length;
      var detailLayers = new int[layerCount][,];
      
      for (int layer = 0; layer < layerCount; layer++) {
        detailLayers[layer] = new int[resolution, resolution];
      }
      
      // Paint vegetation
      for (int y = 0; y < resolution; y++) {
        if (y % 50 == 0) {
          ReportProgress((float)y / resolution, $"Row {y}/{resolution}");
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
          
          // Apply each vegetation layer from biome config
          var vegConfig = biome.vegetationConfig;
          if (vegConfig.layers == null) continue;
          
          foreach (var vegLayer in vegConfig.layers) {
            if (vegLayer?.prototype == null) continue;
            
            // Find matching detail prototype index
            var protoIndex = FindPrototypeIndex(detailPrototypes, vegLayer.prototype);
            if (protoIndex < 0) continue;
            
            // Calculate density with some noise
            var density = vegLayer.density;
            
            // Add random variation
            var noise = (float)ctx.Random.NextDouble();
            if (noise > density) continue;
            
            // Set detail density (0-16 range)
            var detailValue = Mathf.RoundToInt(density * 16f * noise);
            detailLayers[protoIndex][y, x] = detailValue;
          }
        }
      }
      
      ReportProgress(0.9f, "Applying detail layers...");
      
      // Apply all layers
      ctx.DetailLayers = detailLayers;
      for (int i = 0; i < layerCount; i++) {
        td.SetDetailLayer(0, 0, i, detailLayers[i]);
      }
      
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

    protected override Material CreateDebugMaterial(GenerationContext ctx) {
      // No special debug material - vegetation is visible
      return null;
    }

    private int FindPrototypeIndex(DetailPrototype[] prototypes, Vegetation.VegetationPrototypeSO vegProto) {
      // Try to match by prefab
      var prefab = vegProto.prefab;
      if (prefab == null && vegProto.prefabs != null && vegProto.prefabs.Length > 0) {
        prefab = vegProto.prefabs[0];
      }
      
      if (prefab == null) return -1;
      
      for (int i = 0; i < prototypes.Length; i++) {
        if (prototypes[i].prototype == prefab) return i;
      }
      
      return -1;
    }
  }
}
