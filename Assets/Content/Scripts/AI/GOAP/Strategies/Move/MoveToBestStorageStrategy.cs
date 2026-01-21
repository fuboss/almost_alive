using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.Game.Storage;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies.Move {
  [Serializable]
  public class MoveToBestStorageStrategy : MoveStrategy {
    public MoveToBestStorageStrategy(IGoapAgentCore agent, Func<Vector3> destination) : base(agent, destination) {
    }

    public MoveToBestStorageStrategy() {
    }

    protected override MemorySnapshot GetTargetMemory() {
      if (_agent is not IInventoryAgent inv) return null;
      
      var slotWithHaulableItem =
        inv.inventory.occupiedSlots.Where(s => s.item.descriptionData.tags.Contains(Tag.ITEM)).ToArray();

      MemorySnapshot mem = null;
      foreach (var slot in slotWithHaulableItem) {
        var snapshot = targetFromMemory?.GetNearest(_agent, ms => {
          var storageComp = ms.target.GetComponent<StorageActor>();
          return storageComp != null && !storageComp.isFull && storageComp.CanDeposit(slot.item);
        });
        if (snapshot == null) {
          continue;
        }

        mem = snapshot;
      }

      return mem;
    }

    public override IActionStrategy Create(IGoapAgentCore agent) {
      var dest = _destination;
      if (targetFromMemory != null) {
        dest = () => targetFromMemory.Search(agent).location;
      }

      if (dest == null) {
        Debug.LogError("MoveStrategy Create: No destination set!");
        dest = () => agent.position;
      }

      return new MoveToBestStorageStrategy(agent, dest) {
        updateDestinationContinuously = updateDestinationContinuously,
        targetFromMemory = targetFromMemory
      };
    }
  }
}
