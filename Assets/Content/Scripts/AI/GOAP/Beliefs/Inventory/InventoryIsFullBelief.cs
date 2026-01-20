using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Inventory {
  [Serializable, TypeInfoBox("True when agent's inventory is full (no free slots).")]
  public class InventoryIsFullBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => agent.inventory.isFull;
    }

    public override AgentBelief Copy() {
      return new InventoryIsFullBelief {
        name = name
      };
    }
  }
}
