using System.Linq;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [CreateAssetMenu(menuName = "GOAP/Utility/InventoryItemMissing_Composite")]
  public class CompositeUtilityInventoryItemMissingCountSO : CompositeUtilityByTagsSO<InventoryMissingItemUtilityEvaluator,
    InventoryMissingItemUtilitySO> {
    protected override InventoryMissingItemUtilitySO FindExisting(string tag)
      => evaluators.FirstOrDefault(e => e.evaluator.tags.Contains(tag)) as InventoryMissingItemUtilitySO;

    protected override void Init(InventoryMissingItemUtilityEvaluator ev, string tag) {
      ev.tags = new[] { tag };
      ev.requiredCount = 1;
    }
  }
}