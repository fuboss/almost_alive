using Content.Scripts.AI.GOAP.Beliefs.Inventory;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [CreateAssetMenu(fileName = "Belief", menuName = "GOAP/BeliefComposite/Has InInventory", order = 0)]
  public class CompositeInventoryHasBeliefsSO : CompositeBeliefSO<HasInInventoryBelief> {
    protected override HasInInventoryBelief CreateBeliefForTag(string tag) {
      return new HasInInventoryBelief {
         name = $"{name}/{tag}",
        tags = new[] { tag },
        requiredItemCount = 1
      };
    }
  }
}