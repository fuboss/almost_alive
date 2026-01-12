using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs.Inventory {
  [Serializable]
  public class HasFreeSlotsInInventoryBelief : AgentBelief {
    public int requiredItemCount = 1;

    public override bool Evaluate(IGoapAgent agent) {
      condition = () => agent.inventory.freelots.Count() > requiredItemCount;

      return base.Evaluate(agent);
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