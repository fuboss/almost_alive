using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Inventory {
  [Serializable, TypeInfoBox("True when agent has item with specified tags in inventory (or inverse: doesn't have).")]
  public class HasInInventoryBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;
    public int requiredItemCount = 1;
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      if (agent is not IInventoryAgent inv) {
        return () => inverse; // No inventory = no items = inverse logic
      }
      
      return () => {
        var hasEnough = inv.inventory.TryGetSlotWithItemTags(tags, out var slot) && slot.count >= requiredItemCount;
        return !inverse ? hasEnough : !hasEnough;
      };
    }

    public override AgentBelief Copy() {
      return new HasInInventoryBelief {
        tags = tags,
        name = name,
        requiredItemCount = requiredItemCount,
        inverse = inverse
      };
    }
  }
}
