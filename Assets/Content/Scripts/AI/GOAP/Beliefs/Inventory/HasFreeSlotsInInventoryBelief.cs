using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs.Inventory {
  [Serializable]
  public class HasFreeSlotsInInventoryBelief : AgentBelief {
    public int requiredItemCount = 1;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => agent.inventory.freeSlots.Count() > requiredItemCount;
    }

    public override AgentBelief Copy() {
      var copy = new HasFreeSlotsInInventoryBelief {
        name = name,
        requiredItemCount = requiredItemCount,
        condition = condition
      };
      return copy;
    }
  }
}