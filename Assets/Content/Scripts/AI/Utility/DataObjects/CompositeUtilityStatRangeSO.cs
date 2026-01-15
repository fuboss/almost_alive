using System.Linq;
using Content.Scripts.AI.GOAP.Stats;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [CreateAssetMenu(menuName = "GOAP/Utility/StatRange_Composite")]
  public class CompositeUtilityStatRangeSO : CompositeUtilityByStatsSO<StatRangeUtilityEvaluator,
    AgentStatRangeUtilitySO> {
    protected override AgentStatRangeUtilitySO FindExisting(StatType statType)
      => evaluators.FirstOrDefault(e => e.evaluator.statType == statType) as AgentStatRangeUtilitySO;

    protected override void Init(StatRangeUtilityEvaluator ev, StatType tag) {
      ev.statType = tag;
      ev.range = new Vector2(0, 1);
    }
  }
}