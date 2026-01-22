using System;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class InventoryFreeSlotsUtilityEvaluator : EvaluatorBase {
    public int desiredFreeSlots = 1;

    public AnimationCurve slotsCurve =
      AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override float Evaluate(IGoapAgentCore agent) {
      if (agent is not IInventoryAgent inv) return 0f;
      
      var free = inv.inventory.freeSlotCount;
      if (free <= 0) {
        return slotsCurve.Evaluate(0);
      }

      float t = Mathf.Clamp01((float)free / desiredFreeSlots);
      return slotsCurve.Evaluate(t);
    }
  }
}
