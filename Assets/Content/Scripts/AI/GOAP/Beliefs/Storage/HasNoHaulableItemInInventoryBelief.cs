using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  [Serializable, TypeInfoBox("True when agent has no haulable items in inventory (nothing any storage needs).")]
  public class HasNoHaulableItemInInventoryBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        foreach (var slot in agent.inventory.occupiedSlots) {
          if (StorageQuery.AnyStorageNeeds(slot.item)) {
            return false;
          }
        }
        return true;
      };
    }

    public override AgentBelief Copy() {
      return new HasNoHaulableItemInInventoryBelief {
        name = name
      };
    }
  }
}
