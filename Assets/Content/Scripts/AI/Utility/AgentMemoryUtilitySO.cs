using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class MemoryUtilityEvaluator : EvaluatorBase {
    [ValueDropdown("Tags")] public string[] tags;
    public float maxDistance = 20f;

    public AnimationCurve distanceCurve =
      AnimationCurve.EaseInOut(0, 1, 1, 0);

    public override float Evaluate(IGoapAgent agent) {
      var memory = agent.memory;
      var items = memory.GetWithAllTags(tags);

      if (items.Length == 0)
        return 0f;

      var best = 0f;

      foreach (var item in items) {
        var distance = Vector3.Distance(agent.position, item.location);
        var t = Mathf.Clamp01(distance / maxDistance);
        best = Mathf.Max(best, distanceCurve.Evaluate(t));
      }

      return best;
    }
  }

  [CreateAssetMenu(menuName = "GOAP/Utility/Memory")]
  public class AgentMemoryUtilitySO : UtilitySO<MemoryUtilityEvaluator> {
  }
}