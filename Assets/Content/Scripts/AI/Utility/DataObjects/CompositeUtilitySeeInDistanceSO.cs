using System.Linq;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [CreateAssetMenu(menuName = "GOAP/Utility/SeeInDistance_Composite")]
  public class CompositeUtilitySeeInDistanceSO : CompositeUtilityByTagsSO<VisionTagUtilityEvaluator,
    VisionTagUtilitySO> {
    public float distance = 50;

    protected override VisionTagUtilitySO FindExisting(string tag)
      => evaluators.FirstOrDefault(e => e.evaluator.tags.Contains(tag)) as VisionTagUtilitySO;

    protected override void Init(VisionTagUtilityEvaluator ev, string tag) {
      ev.tags = new[] { tag };
      ev.maxDistance = distance;
    }
  }
}