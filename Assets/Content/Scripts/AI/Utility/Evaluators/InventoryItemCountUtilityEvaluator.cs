using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class InventoryItemCountUtilityEvaluator : EvaluatorBase {
    [ValueDropdown("Tags")] public string[] tags;
    public int desiredCount = 1;

    public AnimationCurve countCurve =
      AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override float Evaluate(IGoapAgentCore agent) {
      if (agent is not IInventoryAgent inv) return 0f;
      
      var count = inv.inventory.GetTotalCountWithTags(tags);
      if (count <= 0) return 0;
      var t = Mathf.Clamp01((float)count / desiredCount);
      return countCurve.Evaluate(t);
    }
  }
}
