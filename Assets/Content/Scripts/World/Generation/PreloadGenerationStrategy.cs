using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Content.Scripts.World.Generation {
  /// <summary>
  /// Preload generation strategy.
  /// Loads spawn data from DevPreloadWorld component instead of generating.
  /// </summary>
  public class PreloadGenerationStrategy : IWorldGenerationStrategy {
    private readonly DevPreloadWorld _preload;

    public PreloadGenerationStrategy(DevPreloadWorld preload) {
      _preload = preload;
    }

    public async UniTask ExecuteAsync(WorldGenerationContext context) {
      var config = context.config;

      // Use seed from preload
      context.seed = _preload.seed;
      if (config.logGeneration) {
        Debug.Log($"[PreloadGenerationStrategy] Loading from preload with seed {context.seed}");
      }

      // TODO: Restore BiomeMap from preload when serialization is implemented
      context.biomeMap = null;

      // Generate feature map from existing terrain (fast operation)
      context.onProgress?.Invoke(0.1f);
      context.featureMap = TerrainFeatureMap.Generate(context.terrain);
      config.cachedFeatureMap = context.featureMap;

      // Copy spawn data from preload
      context.onProgress?.Invoke(0.2f);
      context.spawnDataList.Clear();
      context.spawnDataList.AddRange(_preload.spawnDataList);

      if (config.logGeneration) {
        Debug.Log($"[PreloadGenerationStrategy] Loaded {context.spawnDataList.Count} spawn entries from preload");
      }

      await UniTask.Yield(context.cancellationToken);
    }
  }
}

