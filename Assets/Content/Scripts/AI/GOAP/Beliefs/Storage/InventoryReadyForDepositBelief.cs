using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  /// <summary>
  /// True when agent has collected enough items for hauling trip.
  /// Conditions: inventory full OR collected minCount OR no more items nearby in memory.
  /// </summary>
  [Serializable]
  public class InventoryReadyForDepositBelief : AgentBelief {
    [MinValue(1)] public int minItemCount = 2;
    public float searchRadius = 30f;
    [ValueDropdown("GetTags")] public string[] itemTags;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        // Full inventory - ready
        if (agent.inventory.isFull) return true;

        // Check how many haulable items we have
        var haulableCount = 0;
        foreach (var slot in agent.inventory.occupiedSlots) {
          if (StorageQuery.AnyStorageNeeds(slot.item)) {
            haulableCount += slot.count;
          }
        }

        // Have enough - ready
        if (haulableCount >= minItemCount) return true;

        // Check if there are more items nearby in memory
        var nearbyItems = agent.memory.GetInRadius(
          agent.position,
          searchRadius,
          itemTags.Length > 0 ? itemTags : new[] { Tag.ITEM },
          m => m.target != null && m.target?.collectable == true
        );

        // No more items nearby - ready (go deposit what we have)
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