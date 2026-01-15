using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs.Inventory {
  [Serializable]
  public class InventoryIsFullBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return ()=>!agent.inventory.freeSlots.Any();
    }

    public override AgentBelief Copy() {
      var copy = new InventoryIsFullBelief {
        name = name,
        condition = condition
      };
      return copy;
    }
  }
}