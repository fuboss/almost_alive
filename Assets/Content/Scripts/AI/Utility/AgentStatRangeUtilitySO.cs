using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [CreateAssetMenu(menuName = "GOAP/Utility/StatRange")]
  public class AgentStatRangeUtilitySO : UtilitySO {
    public StatType statType;

    [MinMaxSlider(0, 1f, true)] public Vector2 range;

    public AnimationCurve responseCurve =
      AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override float Evaluate(IGoapAgent agent) {
      if (agent.body.GetStat(statType) is not FloatAgentStat stat) return 0f;

      var normalized = Mathf.InverseLerp(range.x, range.y, stat.Normalized);
      return responseCurve.Evaluate(normalized);
    }
  }
}