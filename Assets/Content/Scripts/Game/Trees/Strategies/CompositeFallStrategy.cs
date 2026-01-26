using UnityEngine;

namespace Content.Scripts.Game.Trees.Strategies {
  public class CompositeFallStrategy : ITreeFallDirectionStrategy {
    private readonly ITreeFallDirectionStrategy _primaryStrategy;
    private readonly ITreeFallDirectionStrategy _fallbackStrategy;
    private readonly float _primaryProbability;

    public CompositeFallStrategy(
      ITreeFallDirectionStrategy primaryStrategy,
      ITreeFallDirectionStrategy fallbackStrategy,
      float primaryProbability = 0.3f
    ) {
      _primaryStrategy = primaryStrategy;
      _fallbackStrategy = fallbackStrategy;
      _primaryProbability = primaryProbability;
    }

    public Vector3 GetFallDirection(Transform tree, Vector3 chopperPosition, TreeFallConfigSO config) {
      if (Random.value < _primaryProbability) {
        return _primaryStrategy.GetFallDirection(tree, chopperPosition, config);
      }
      return _fallbackStrategy.GetFallDirection(tree, chopperPosition, config);
    }
  }
}
