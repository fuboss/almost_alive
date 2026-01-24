using Cysharp.Threading.Tasks;

namespace Content.Scripts.World.Generation {
  /// <summary>
  /// Strategy interface for world generation.
  /// Implementations define how the world is generated or loaded.
  /// </summary>
  public interface IWorldGenerationStrategy {
    /// <summary>
    /// Execute the generation strategy.
    /// Should populate context.SpawnDataList with actors to spawn.
    /// </summary>
    UniTask ExecuteAsync(WorldGenerationContext context);
  }
}

