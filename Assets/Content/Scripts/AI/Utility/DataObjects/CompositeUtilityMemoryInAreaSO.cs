using System.Linq;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [CreateAssetMenu(menuName = "GOAP/Utility/MemoryInArea_Composite")]
  public class CompositeUtilityMemoryInAreaSO : CompositeUtilityByTagsSO<MemoryUtilityEvaluator,
    AgentMemoryUtilitySO> {
    public float distance = 50;

    protected override AgentMemoryUtilitySO FindExisting(string tag)
      => evaluators.FirstOrDefault(e => e.evaluator.tags.Contains(tag)) as AgentMemoryUtilitySO;

    protected override void Init(MemoryUtilityEvaluator ev, string tag) {
      ev.tags = new[] { tag };
      ev.maxDistance = distance;
    }
  }
}