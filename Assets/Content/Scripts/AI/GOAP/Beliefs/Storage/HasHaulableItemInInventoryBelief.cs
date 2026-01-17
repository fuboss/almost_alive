using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Beliefs.Inventory;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  [Serializable, TypeInfoBox("True when agent has item in inventory that some storage needs.")]
  public class HasHaulableItemInInventoryBelief : HasInInventoryBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var items = agent.inventory.occupiedSlots.Where(s => s.item.collectable).ToArray();
        return items.Any(slot => StorageQuery.AnyStorageNeeds(slot.item));
      };
    }

    public override AgentBelief Copy() {
      return new HasHaulableItemInInventoryBelief {
        tags = tags,
        name = name,
        requiredItemCount = requiredItemCount,
      };
    }
  }
}
