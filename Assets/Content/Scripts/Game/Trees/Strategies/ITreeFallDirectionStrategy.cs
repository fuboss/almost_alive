using UnityEngine;

namespace Content.Scripts.Game.Trees.Strategies {
  public interface ITreeFallDirectionStrategy {
    Vector3 GetFallDirection(Transform tree, Vector3 chopperPosition, TreeFallConfigSO config);
  }
}
