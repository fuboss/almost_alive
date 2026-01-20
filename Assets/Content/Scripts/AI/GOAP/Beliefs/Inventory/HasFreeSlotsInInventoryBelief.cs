using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Inventory {
  [Serializable, TypeInfoBox("True when agent has free slots in inventory.")]
  public class HasFreeSlotsInInventoryBelief : AgentBelief {
    public int requiredItemCount = 1;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => agent.inventory.freeSlotCount >= requiredItemCount;
    }

    public override AgentBelief Copy() {
      return new HasFreeSlotsInInventoryBelief {
        name = name,
        requiredItemCount = requiredItemCount
      };
    }
  }
}
