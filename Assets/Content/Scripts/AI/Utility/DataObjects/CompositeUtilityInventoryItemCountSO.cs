using System.Linq;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [CreateAssetMenu(menuName = "GOAP/Utility/InventoryItemCount_Composite")]
  public class CompositeUtilityInventoryItemCountSO : CompositeUtilityByTagsSO<InventoryItemCountUtilityEvaluator,
    InventoryItemCountUtilitySO> {
    protected override InventoryItemCountUtilitySO FindExisting(string tag)
      => evaluators.FirstOrDefault(e => e.evaluator.tags.Contains(tag)) as InventoryItemCountUtilitySO;

    protected override void Init(InventoryItemCountUtilityEvaluator ev, string tag) {
      ev.tags = new[] { tag };
      ev.desiredCount = 1;
    }
  }
}