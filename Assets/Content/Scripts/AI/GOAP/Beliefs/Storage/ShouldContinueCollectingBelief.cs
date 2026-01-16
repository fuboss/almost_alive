using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  /// <summary>
  /// Inverse of InventoryReadyForDepositBelief.
  /// True when agent should continue collecting items.
  /// </summary>
  [Serializable]
  public class ShouldContinueCollectingBelief : AgentBelief {
    [MinValue(1)] public int minItemCount = 2;
    public float searchRadius = 30f;
    [ValueDropdown("GetTags")] public string[] itemTags;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        // Full inventory - stop collecting
        if (agent.inventory.isFull) return false;

        // Check how many haulable items we have
        var haulableCount = 0;
        foreach (var slot in agent.inventory.occupiedSlots) {
          if (StorageQuery.AnyStorageNeeds(slot.item)) {
            haulableCount += slot.count;
          }
        }

        // Have enough - stop collecting
        if (haulableCount >= minItemCount) return false;

        // Check if there are more items nearby in memory
        var nearbyItems = agent.memory.GetInRadius(
          agent.position,
          searchRadius,
          itemTags.Length > 0 ? itemTags : new[] { Tag.ITEM },
          m => m.target != null && m.target?.collectable == true
        );

        // Has items nearby - continue collecting
        return nearbyItems.Length > 0;
      };
    }

    public override AgentBelief Copy() {
      return new ShouldContinueCollectingBelief {
        name = name,
        minItemCount = minItemCount,
        searchRadius = searchRadius,
        itemTags = itemTags
      };
    }
  }
}
