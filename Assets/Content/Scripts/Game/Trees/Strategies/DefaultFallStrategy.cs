using UnityEngine;

namespace Content.Scripts.Game.Trees.Strategies {
  public class DefaultFallStrategy : ITreeFallDirectionStrategy {
    private readonly TowardsBuildingsStrategy _buildingsStrategy = new();
    private readonly AwayFromChopperStrategy _awayStrategy = new();
    private readonly RandomFallStrategy _randomStrategy = new();

    public Vector3 GetFallDirection(Transform tree, Vector3 chopperPosition, TreeFallConfigSO config) {
      if (Random.value < config.Data.buildingTargetProbability) {
        return _buildingsStrategy.GetFallDirection(tree, chopperPosition, config);
      }

      if (Random.value < 0.7f) {
        return _awayStrategy.GetFallDirection(tree, chopperPosition, config);
      }

      return _randomStrategy.GetFallDirection(tree, chopperPosition, config);
    }
  }
}
