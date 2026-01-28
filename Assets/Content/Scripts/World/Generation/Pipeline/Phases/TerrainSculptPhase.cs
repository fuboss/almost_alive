using UnityEngine;

namespace Content.Scripts.World.Generation.Pipeline.Phases {
  /// <summary>
  /// Phase 2: Sculpt terrain heightmap based on biome settings.
  /// Applies height noise from each biome with proper blending.
  /// </summary>
  public class TerrainSculptPhase : GenerationPhaseBase {
    
    public override string Name => "Terrain Sculpt";
    public override string Description => "Apply heightmap based on biomes";

    protected override bool ValidateContext(GenerationContext ctx) {
      if (ctx?.BiomeMap == null) {
        Debug.LogError("[TerrainSculpt] BiomeMap not generated");
        return false;
      }
      if (!ctx.Config.sculptTerrain) {
        Debug.Log("[TerrainSculpt] Skipped (sculptTerrain disabled)");
        return false;
      }
      return true;
    }

    protected override void ExecuteInternal(GenerationContext ctx) {
      ReportProgress(0f, "Sculpting terrain...");
      
      var terrain = ctx.Terrain;
      var td = terrain.terrainData;
      var resolution = td.heightmapResolution;
      var heights = new float[resolution, resolution];
      var biomeMap = ctx.BiomeMap;
      var bounds = ctx.Bounds;
      
      // Sample each point
      for (int y = 0; y < resolution; y++) {
        if (y % 50 == 0) {
          ReportProgress((float)y / resolution, $"Row {y}/{resolution}");
        }
        
        for (int x = 0; x < resolution; x++) {
          // Convert heightmap coords to world position
          var nx = x / (float)(resolution - 1);
          var ny = y / (float)(resolution - 1);
          
          var worldX = bounds.min.x + nx * bounds.size.x;
          var worldZ = bounds.min.z + ny * bounds.size.z;
          var worldPos = new Vector3(worldX, 0, worldZ);
          
          // Get biome data at this point (returns BiomeSO, not BiomeType)
          var biome = biomeMap.GetBiomeDataAt(worldPos);
          
          // Calculate height from biome
          float height = 0f;
          
          if (biome != null) {
            // Base height from biome (normalized 0-1 for terrain)
            height = biome.baseHeight / td.size.y;
            
            // Add noise if biome has heightNoise configured
            if (biome.heightNoise != null) {
              biome.heightNoise.SetSeed(ctx.Seed);
              var noiseValue = biome.heightNoise.Sample(worldX, worldZ);
              height += noiseValue * (biome.heightVariation / td.size.y);
            }
          }
          
          // Clamp to valid range
          heights[y, x] = Mathf.Clamp01(height);
        }
      }
      
      ReportProgress(0.9f, "Applying heightmap...");
      
      // Store for potential rollback
      ctx.Heightmap = heights;
      
      // Apply to terrain
      td.SetHeights(0, 0, heights);
      
      ReportProgress(1f);
      ClearProgressBar();
    }

    protected override void RollbackInternal(GenerationContext ctx) {
      if (ctx.OriginalHeightmap != null) {
        ctx.Terrain.terrainData.SetHeights(0, 0, ctx.OriginalHeightmap);
      }
      ctx.Heightmap = null;
    }

    protected override Material CreateDebugMaterial(GenerationContext ctx) {
      // Terrain shows actual heights, no special debug material needed
      return null;
    }
  }
}
