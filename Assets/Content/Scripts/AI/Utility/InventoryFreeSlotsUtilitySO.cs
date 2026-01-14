using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [CreateAssetMenu(menuName = "GOAP/Utility/InventoryFreeSlots")]
  public class InventoryFreeSlotsUtilitySO : UtilitySO<InventoryFreeSlotsUtilityEvaluator> {
  }

  [Serializable]
  public class InventoryFreeSlotsUtilityEvaluator : EvaluatorBase {
    public int desiredFreeSlots = 1;

    public AnimationCurve slotsCurve =
      AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override float Evaluate(IGoapAgent agent) {
      var free = agent.inventory.freelots.Count();
      if (free <= 0) {
        return slotsCurve.Evaluate(0);
      }

      float t = Mathf.Clamp01((float)free / desiredFreeSlots);
      return slotsCurve.Evaluate(t);
    }
  }
}