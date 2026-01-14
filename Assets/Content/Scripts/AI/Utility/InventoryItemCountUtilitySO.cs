using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [CreateAssetMenu(menuName = "GOAP/Utility/InventoryItemCount")]
  public class InventoryItemCountUtilitySO : UtilitySO<InventoryItemCountUtilityEvaluator> {
  }

  [Serializable]
  public class InventoryItemCountUtilityEvaluator : EvaluatorBase {
    [ValueDropdown("Tags")] public string[] tags;
    public int desiredCount = 1;

    public AnimationCurve countCurve =
      AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override float Evaluate(IGoapAgent agent) {
      var count = agent.inventory.GetTotalCountWithTags(tags);
      if (count <= 0) return 0;
      var t = Mathf.Clamp01((float)count / desiredCount);
      return countCurve.Evaluate(t);
    }
  }
}