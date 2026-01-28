using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World.Generation.Pipeline.Phases {
  /// <summary>
  /// Phase 1: Generate biome regions using Voronoi diagram.
  /// Creates BiomeMap with cell boundaries and biome assignments.
  /// </summary>
  public class BiomeLayoutPhase : GenerationPhaseBase {
    
    public override string Name => "Biome Layout";
    public override string Description => "Generate Voronoi biome regions";

    private static Material _debugMaterial;
    private static Texture2D _debugTexture;

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
      
      // Generate Voronoi biome map
      var biomeMap = VoronoiGenerator.Generate(
        ctx.Bounds,
        config.biomes,
        config.biomeBorderBlend,
        ctx.Seed,
        config.minBiomeCells,
        config.maxBiomeCells
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

    protected override Material CreateDebugMaterial(GenerationContext ctx) {
      if (ctx.BiomeMap == null) return null;
      
      // Create or update debug texture
      _debugTexture = GenerateBiomeColorTexture(ctx.BiomeMap, 256);
      
      // Create simple unlit material
      if (_debugMaterial == null) {
        _debugMaterial = new Material(Shader.Find("Unlit/Texture"));
      }
      
      _debugMaterial.mainTexture = _debugTexture;
      return _debugMaterial;
    }

    private static Texture2D GenerateBiomeColorTexture(BiomeMap map, int resolution) {
      var tex = new Texture2D(resolution, resolution, TextureFormat.RGB24, false) {
        filterMode = FilterMode.Point, // Sharp edges for debug viz
        wrapMode = TextureWrapMode.Clamp
      };
      
      var pixels = new Color[resolution * resolution];
      var bounds = map.bounds;
      
      for (int y = 0; y < resolution; y++) {
        for (int x = 0; x < resolution; x++) {
          // Map texture coords to world position
          var worldX = bounds.min.x + (x / (float)(resolution - 1)) * bounds.size.x;
          var worldZ = bounds.min.z + (y / (float)(resolution - 1)) * bounds.size.z;
          var worldPos = new Vector3(worldX, 0, worldZ);
          
          // Get biome data at this position (returns BiomeSO, not BiomeType)
          var biome = map.GetBiomeDataAt(worldPos);
          var color = biome != null ? biome.debugColor : Color.white;
          
          pixels[y * resolution + x] = color;
        }
      }
      
      tex.SetPixels(pixels);
      tex.Apply();
      return tex;
    }
  }
}
