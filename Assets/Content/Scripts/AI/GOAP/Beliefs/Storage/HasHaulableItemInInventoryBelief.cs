using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  [Serializable, TypeInfoBox("True when agent has item in inventory that some storage needs.")]
  public class HasHaulableItemInInventoryBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        foreach (var slot in agent.inventory.occupiedSlots) {
          if (slot.item.collectable && StorageQuery.AnyStorageNeeds(slot.item)) {
            return true;
          }
        }
        return false;
      };
    }

    public override AgentBelief Copy() {
      return new HasHaulableItemInInventoryBelief {
        name = name
      };
    }
  }
}
