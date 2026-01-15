using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Storage;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class StorageFillNeedUtilityEvaluator : EvaluatorBase {
    public float basePriority = 0.3f;

    public AnimationCurve evaluationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public override float Evaluate(IGoapAgent agent) {
      var maxPriority = 0;
      var totalFreeSlots = 0;

      foreach (var storage in ActorRegistry<StorageActor>.all) {
        if (!storage.priority.isEnabled || storage.isFull) continue;
        maxPriority = Mathf.Max(maxPriority, storage.priority.priority);
        totalFreeSlots += storage.freeSlots;
      }

      if (totalFreeSlots == 0) return 0f;

      var priorityFactor = maxPriority / 8f;
      var slotsFactor = Mathf.Clamp01(totalFreeSlots / 8f);

      var result = evaluationCurve.Evaluate(Mathf.Clamp01(basePriority * priorityFactor * (0.5f + slotsFactor * 0.5f)));
      return result;
    }
  }
}