using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World.Generation.Pipeline.Phases {
  /// <summary>
  /// Phase 1: Generate biome regions using Voronoi diagram.
  /// Creates BiomeMap with cell boundaries and biome assignments.
  /// Debug visualization handled by BiomeOverlayGizmoDrawer (Editor).
  /// </summary>
  public class BiomeLayoutPhase : GenerationPhaseBase {
    
    public override string Name => "Biome Layout";
    public override string Description => "Generate Voronoi biome regions";

    protected override bool ValidateContext(GenerationContext ctx) {
      if (ctx?.Config == null) return false;
      if (ctx.Config.biomes == null || ctx.Config.biomes.Count == 0) {
        Debug.LogError("[BiomeLayout] No biomes configured");
        return false;
      }
      return true;
    }

    protected override void ExecuteInternal(GenerationContext ctx) {
      ReportProgress(0f, "Generating Voronoi cells...");
      
      var config = ctx.Config;
      
      // Generate Voronoi biome map with domain warping
      var biomeMap = VoronoiGenerator.Generate(
        ctx.Bounds,
        config.biomes,
        config.biomeBorderBlend,
        ctx.Seed,
        config.minBiomeCells,
        config.maxBiomeCells,
        config.useDomainWarping,
        config.warpStrength,
        config.warpScale,
        config.warpOctaves
      );
      
      ReportProgress(0.8f, "Storing biome map...");
      
      ctx.BiomeMap = biomeMap;
      
      // Cache in config SO for gizmo drawing
      ctx.ConfigSO.cachedBiomeMap = biomeMap;
      
      ReportProgress(1f);
      ClearProgressBar();
    }

    protected override void RollbackInternal(GenerationContext ctx) {
      ctx.BiomeMap = null;
      ctx.ConfigSO.cachedBiomeMap = null;
    }
  }
}
