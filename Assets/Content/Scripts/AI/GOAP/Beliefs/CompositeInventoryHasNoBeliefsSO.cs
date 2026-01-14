using Content.Scripts.AI.GOAP.Beliefs.Inventory;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [CreateAssetMenu(fileName = "Belief", menuName = "GOAP/BeliefComposite/Has No InInventory", order = 0)]
  public class CompositeInventoryHasNoBeliefsSO : CompositeBeliefSO<HasNoInInventoryBelief> {
    protected override HasNoInInventoryBelief CreateBeliefForTag(string tag) {
      return new HasNoInInventoryBelief {
         name = $"{name}/{tag}",
        tags = new[] { tag },
        requiredItemCount = 1
      };
    }
  }
}