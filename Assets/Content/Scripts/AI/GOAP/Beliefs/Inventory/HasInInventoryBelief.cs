using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Inventory {
  [Serializable]
  public class HasInInventoryBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;
    public int requiredItemCount = 1;

    public override bool Evaluate(IGoapAgent agent) {
      condition = () => agent.inventory.TryGetItemWithTags(tags, out var slot) && slot.count >= requiredItemCount;

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy() {
      var copy = new HasInInventoryBelief {
        tags = tags,
        name = name,
        requiredItemCount = requiredItemCount,
        condition = condition
      };
      return copy;
    }
  }
}