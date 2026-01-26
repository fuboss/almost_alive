using UnityEngine;

namespace Content.Scripts.Game.Trees.Strategies {
  public class AwayFromChopperStrategy : ITreeFallDirectionStrategy {
    public Vector3 GetFallDirection(Transform tree, Vector3 chopperPosition, TreeFallConfigSO config) {
      var dir = tree.position - chopperPosition;
      dir.y = 0;
      if (dir.sqrMagnitude < 0.01f) {
        var random2D = Random.insideUnitCircle.normalized;
        return new Vector3(random2D.x, 0, random2D.y);
      }
      return dir.normalized;
    }
  }
}
