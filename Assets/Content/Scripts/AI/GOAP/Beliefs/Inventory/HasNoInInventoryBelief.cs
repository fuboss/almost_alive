using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Inventory {
  [Serializable]
  public class HasNoInInventoryBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;
    public int requiredItemCount = 1;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => !agent.inventory.TryGetSlotWithItemTags(tags, out var slot) || slot.count < requiredItemCount;
    }

    public override AgentBelief Copy() {
      var copy = new HasNoInInventoryBelief {
        tags = tags,
        name = name,
        requiredItemCount = requiredItemCount,
        condition = condition
      };
      return copy;
    }
  }
}