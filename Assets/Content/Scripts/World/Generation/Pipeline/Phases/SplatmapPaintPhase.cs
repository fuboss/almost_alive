using UnityEngine;

namespace Content.Scripts.World.Generation.Pipeline.Phases {
  /// <summary>
  /// Phase 3: Paint terrain splatmap textures based on biomes.
  /// Applies base textures and handles biome border blending.
  /// </summary>
  public class SplatmapPaintPhase : GenerationPhaseBase {
    
    public override string Name => "Splatmap Paint";
    public override string Description => "Paint terrain textures";

    protected override bool ValidateContext(GenerationContext ctx) {
      if (ctx?.BiomeMap == null) {
        Debug.LogError("[SplatmapPaint] BiomeMap not generated");
        return false;
      }
      if (!ctx.Config.paintSplatmap) {
        Debug.Log("[SplatmapPaint] Skipped (paintSplatmap disabled)");
        return false;
      }
      return true;
    }

    protected override void ExecuteInternal(GenerationContext ctx) {
      ReportProgress(0f, "Painting terrain textures...");
      
      var terrain = ctx.Terrain;
      var td = terrain.terrainData;
      var resolution = td.alphamapResolution;
      var layerCount = td.alphamapLayers;
      
      if (ctx.Config == null) {
        Debug.LogError("[SplatmapPaint] Config is null!");
        return;
      }
      
      if (ctx.Config.terrainPalette == null) {
        Debug.LogError("[SplatmapPaint] TerrainPalette is null in config!");
        return;
      }
      
      if (layerCount == 0) {
        Debug.LogWarning("[SplatmapPaint] No terrain layers - applying palette");
        ctx.Config.terrainPalette.ApplyToTerrain(terrain);
        layerCount = td.alphamapLayers;
        
        if (layerCount == 0) {
          Debug.LogError("[SplatmapPaint] Still no layers after palette apply!");
          return;
        }
      }
      
      var splatmap = new float[resolution, resolution, layerCount];
      var biomeMap = ctx.BiomeMap;
      var bounds = ctx.Bounds;
      
      for (int y = 0; y < resolution; y++) {
        if (y % 50 == 0) {
          ReportProgress((float)y / resolution, $"Row {y}/{resolution}");
        }
        
        for (int x = 0; x < resolution; x++) {
          // Convert splatmap coords to world position
          var nx = x / (float)(resolution - 1);
          var ny = y / (float)(resolution - 1);
          
          var worldX = bounds.min.x + nx * bounds.size.x;
          var worldZ = bounds.min.z + ny * bounds.size.z;
          var worldPos = new Vector3(worldX, 0, worldZ);
          
          // Get BiomeSO at this point
          var biome = biomeMap.GetBiomeDataAt(worldPos);
          
          // Initialize all layers to 0
          for (int i = 0; i < layerCount; i++) {
            splatmap[y, x, i] = 0f;
          }
          
          if (biome != null) {
            var layerIndex = biome.GetBaseLayerIndex();
            
            if (layerIndex >= 0 && layerIndex < layerCount) {
              splatmap[y, x, layerIndex] = 1f;
            } else {
              // Fallback to first layer
              Debug.LogWarning($"[SplatmapPaint] Invalid layer index {layerIndex} for biome {biome.name} (layerName: {biome.baseTexture.layerName})");
              splatmap[y, x, 0] = 1f;
            }
          } else {
            // No biome - use first layer
            splatmap[y, x, 0] = 1f;
          }
        }
      }
      
      ReportProgress(0.9f, "Applying splatmap...");
      
      ctx.Splatmap = splatmap;
      td.SetAlphamaps(0, 0, splatmap);
      
      ReportProgress(1f);
      ClearProgressBar();
    }

    protected override void RollbackInternal(GenerationContext ctx) {
      if (ctx.OriginalSplatmap != null) {
        ctx.Terrain.terrainData.SetAlphamaps(0, 0, ctx.OriginalSplatmap);
      }
      ctx.Splatmap = null;
    }
  }
}
