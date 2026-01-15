using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class InventoryMissingItemUtilityEvaluator : EvaluatorBase {
    [ValueDropdown("Tags")] public string[] tags;
    public int requiredCount = 1;

    public AnimationCurve missingCurve =
      AnimationCurve.EaseInOut(0, 1, 1, 0);

    public override float Evaluate(IGoapAgent agent) {
      var count = agent.inventory.GetTotalCountWithTags(tags);
      if (count <= 0) return missingCurve.Evaluate(0);
      var t = Mathf.Clamp01((float)count / requiredCount);
      return missingCurve.Evaluate(t);
    }
  }
}