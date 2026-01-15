using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Beliefs.Inventory;
using Content.Scripts.Game.Storage;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  [Serializable]
  public class HasNoHaulableItemInInventoryBelief : HasNoInInventoryBelief {

    public override bool Evaluate(IGoapAgent agent) {
      condition = () => {
        return !agent.inventory.occupiedSlots.Any(slot => StorageQuery.AnyStorageNeeds(slot.item));
      };
      return base.Evaluate(agent);
    }

    public override AgentBelief Copy() {
      return new HasNoHaulableItemInInventoryBelief() {
        tags = tags,
        name = name,
        requiredItemCount = requiredItemCount,
      };
    }
  }
}