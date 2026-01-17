using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  [Serializable, TypeInfoBox("True when agent has collected enough items for hauling trip (inventory full OR minCount reached OR no more items nearby).")]
  public class InventoryReadyForDepositBelief : AgentBelief {
    [MinValue(1)] public int minItemCount = 2;
    public float searchRadius = 30f;
    [ValueDropdown("GetTags")] public string[] itemTags;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        if (agent.inventory.isFull) return true;

        var haulableCount = 0;
        foreach (var slot in agent.inventory.occupiedSlots) {
          if (StorageQuery.AnyStorageNeeds(slot.item)) {
            haulableCount += slot.count;
          }
        }

        if (haulableCount >= minItemCount) return true;

        var nearbyItems = agent.memory.GetInRadius(
          agent.position,
          searchRadius,
          itemTags.Length > 0 ? itemTags : new[] { Tag.ITEM },
          m => m.target != null && m.target?.collectable == true
        );

        return nearbyItems.Length == 0 && haulableCount > 0;
      };
    }

    public override AgentBelief Copy() {
      return new InventoryReadyForDepositBelief {
        name = name,
        minItemCount = minItemCount,
        searchRadius = searchRadius,
        itemTags = itemTags
      };
    }
  }
}
