using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class HasInInventoryBelief : AgentBelief {
    public string[] tags;
    public int requiredItemCount = 1;

    public override bool Evaluate(IGoapAgent agent) {
      _condition = () => agent.inventory.TryGetItemWithTags(tags, out var slot) && slot.count >= requiredItemCount;

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy(IGoapAgent agent) {
      var copy = new HasInInventoryBelief {
        tags = tags,
        name = name,
        requiredItemCount = requiredItemCount,
        _condition = _condition
      };
      return copy;
    }
  }

  [Serializable]
  public class HasNoInInventoryBelief : AgentBelief {
    public string[] tags;
    public int requiredItemCount = 1;

    public override bool Evaluate(IGoapAgent agent) {
      _condition = () => !agent.inventory.TryGetItemWithTags(tags, out var slot) || slot.count < requiredItemCount;

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy(IGoapAgent agent) {
      var copy = new HasNoInInventoryBelief {
        tags = tags,
        name = name,
        requiredItemCount = requiredItemCount,
        _condition = _condition
      };
      return copy;
    }
  }

  [Serializable]
  public class HasFreeSlotsInInventoryBelief : AgentBelief {
    public int requiredItemCount = 1;

    public override bool Evaluate(IGoapAgent agent) {
      _condition = () => agent.inventory.freelots.Count() > requiredItemCount;

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy(IGoapAgent agent) {
      var copy = new HasFreeSlotsInInventoryBelief {
        name = name,
        requiredItemCount = requiredItemCount,
        _condition = _condition
      };
      return copy;
    }
  }

  [Serializable]
  public class InventoryIsFullBelief : AgentBelief {
    public override bool Evaluate(IGoapAgent agent) {
      _condition = () => !agent.inventory.freelots.Any();

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy(IGoapAgent agent) {
      var copy = new InventoryIsFullBelief {
        name = name,
        _condition = _condition
      };
      return copy;
    }
  }
}