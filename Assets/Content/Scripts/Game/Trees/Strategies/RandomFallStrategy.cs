using UnityEngine;

namespace Content.Scripts.Game.Trees.Strategies {
  public class RandomFallStrategy : ITreeFallDirectionStrategy {
    public Vector3 GetFallDirection(Transform tree, Vector3 chopperPosition, TreeFallConfigSO config) {
      var random2D = Random.insideUnitCircle.normalized;
      return new Vector3(random2D.x, 0, random2D.y).normalized;
    }
  }
}
