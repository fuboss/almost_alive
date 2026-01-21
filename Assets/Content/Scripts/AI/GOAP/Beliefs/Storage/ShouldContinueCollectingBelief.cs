using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  [Serializable, TypeInfoBox("True when agent should continue collecting items (not full, not enough, items nearby exist).")]
  public class ShouldContinueCollectingBelief : AgentBelief {
    [MinValue(1)] public int minItemCount = 2;
    public float searchRadius = 30f;
    [ValueDropdown("GetTags")] public string[] itemTags;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      if (agent is not IInventoryAgent inv) return () => false;
      
      return () => {
        if (inv.inventory.isFull) return false;

        var haulableCount = 0;
        foreach (var slot in inv.inventory.occupiedSlots) {
          if (StorageQuery.AnyStorageNeeds(slot.item)) {
            haulableCount += slot.count;
          }
        }

        if (haulableCount >= minItemCount) return false;

        var nearbyItems = agent.memory.GetInRadius(
          agent.position,
          searchRadius,
          itemTags.Length > 0 ? itemTags : new[] { Tag.ITEM },
          m => m.target != null && m.target?.collectable == true
        );

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
