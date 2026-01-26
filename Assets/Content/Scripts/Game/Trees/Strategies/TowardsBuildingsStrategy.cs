using Content.Scripts.Building.Runtime;
using UnityEngine;

namespace Content.Scripts.Game.Trees.Strategies {
  public class TowardsBuildingsStrategy : ITreeFallDirectionStrategy {
    public Vector3 GetFallDirection(Transform tree, Vector3 chopperPosition, TreeFallConfigSO config) {
      var structures = ActorRegistry<Structure>.all;
      if (structures.Count == 0) {
        return GetRandomDirection();
      }

      Structure nearest = null;
      float nearestDist = float.MaxValue;
      var treePos = tree.position;

      foreach (var structure in structures) {
        if (structure == null) continue;
        var dist = Vector3.Distance(treePos, structure.transform.position);
        if (dist < config.buildingSearchRadius && dist < nearestDist) {
          nearestDist = dist;
          nearest = structure;
        }
      }

      if (nearest == null) {
        return GetRandomDirection();
      }

      var dir = nearest.transform.position - treePos;
      dir.y = 0;
      return dir.normalized;
    }

    private static Vector3 GetRandomDirection() {
      var random2D = Random.insideUnitCircle.normalized;
      return new Vector3(random2D.x, 0, random2D.y);
    }
  }
}
