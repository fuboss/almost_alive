using Content.Scripts.World.Biomes;
using Content.Scripts.World.Vegetation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Content.Scripts.World.Generation {
  /// <summary>
  /// Preload generation strategy.
  /// Loads spawn data from DevPreloadWorld, regenerates BiomeMap for vegetation.
  /// Terrain heightmap/splatmap are already baked from editor generation.
  /// </summary>
  public class PreloadGenerationStrategy : IWorldGenerationStrategy {
    private readonly DevPreloadWorld _preload;

    public PreloadGenerationStrategy(DevPreloadWorld preload) {
      _preload = preload;
    }

    public async UniTask ExecuteAsync(WorldGenerationContext context) {
      var config = context.config;
      var terrain = context.terrain;
      var ct = context.cancellationToken;

      // Use seed from preload
      context.seed = _preload.seed;
      if (config.ShouldLogGeneration) {
        Debug.Log($"[PreloadStrategy] Loading from preload with seed {context.seed}");
      }

      var bounds = context.GetTerrainBounds();

      // Regenerate BiomeMap (deterministic from seed, needed for vegetation)
      context.onProgress?.Invoke(0.05f);
      context.biomeMap = VoronoiGenerator.Generate(
        bounds, config.Data.biomes, config.Data.biomeBorderBlend, context.seed,
        config.Data.minBiomeCells, config.Data.maxBiomeCells
      );
      config.cachedBiomeMap = context.biomeMap;

      if (config.ShouldLogGeneration) {
        Debug.Log($"[PreloadStrategy] Regenerated BiomeMap with {context.biomeMap?.cells?.Count ?? 0} cells");
      }

      // Generate feature map from existing terrain
      context.onProgress?.Invoke(0.15f);
      context.featureMap = TerrainFeatureMap.Generate(terrain);
      config.cachedFeatureMap = context.featureMap;
      
      await UniTask.Yield(ct);

      // Paint vegetation (terrain heightmap/splatmap already baked from editor)
      if (config.Data.paintVegetation && context.biomeMap != null) {
        context.onProgress?.Invoke(0.25f);
        VegetationPainter.Paint(terrain, context.biomeMap, config.Data.biomes, context.seed);
        
        if (config.ShouldLogGeneration) {
          Debug.Log("[PreloadStrategy] Vegetation painted");
        }
      }

      await UniTask.Yield(ct);

      // Copy spawn data from preload
      context.onProgress?.Invoke(0.40f);
      context.spawnDataList.Clear();
      context.spawnDataList.AddRange(_preload.spawnDataList);

      if (config.ShouldLogGeneration) {
        Debug.Log($"[PreloadStrategy] Loaded {context.spawnDataList.Count} spawn entries");
      }
    }
  }
}
