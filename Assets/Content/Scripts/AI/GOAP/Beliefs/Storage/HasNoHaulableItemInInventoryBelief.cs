using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Beliefs.Inventory;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  [Serializable, TypeInfoBox("True when agent has no haulable items in inventory (nothing any storage needs).")]
  public class HasNoHaulableItemInInventoryBelief : HasNoInInventoryBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        return !agent.inventory.occupiedSlots.Any(slot => StorageQuery.AnyStorageNeeds(slot.item));
      };
    }

    public override AgentBelief Copy() {
      return new HasNoHaulableItemInInventoryBelief {
        tags = tags,
        name = name,
        requiredItemCount = requiredItemCount,
      };
    }
  }
}
