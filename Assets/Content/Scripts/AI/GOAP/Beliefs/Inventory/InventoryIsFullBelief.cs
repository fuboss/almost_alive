using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs.Inventory {
  [Serializable]
  public class InventoryIsFullBelief : AgentBelief {
    public override bool Evaluate(IGoapAgent agent) {
      condition = () => !agent.inventory.freelots.Any();

      return base.Evaluate(agent);
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