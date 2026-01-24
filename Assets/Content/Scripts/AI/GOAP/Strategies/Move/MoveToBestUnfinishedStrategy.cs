using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Content.Scripts.Game.Storage;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies.Move {
  [Serializable]
  public class MoveToBestUnfinishedStrategy : MoveStrategy {
    public MoveToBestUnfinishedStrategy(IGoapAgentCore agent, Func<Vector3> destination) : base(agent, destination) {
    }

    public MoveToBestUnfinishedStrategy() {
    }

    protected override MemorySnapshot GetTargetMemory() {
      var unfinished = ActorRegistry<UnfinishedActor>.all.FirstOrDefault();
      if (unfinished != null) {
        return _agent.memory.RecallTarget(unfinished.actor);
      }

      return null;
      if (_agent is not IInventoryAgent inv) return null;
      //todo: move this part into a separate strategy class
      
      // var slotWithHaulableItem = inv.inventory.occupiedSlots
      //   .Where(s => s.item.descriptionData.tags.Contains(Tag.ITEM))
      //   .ToArray();
      //
      // MemorySnapshot mem = null;
      // foreach (var slot in slotWithHaulableItem) {
      //   var snapshot = targetFromMemory?.GetNearest(_agent, ms => {
      //     if (ms.target == null) return false;
      //     var storageComp = ms.target.GetComponent<StorageActor>();
      //     return storageComp != null && !storageComp.isFull && storageComp.CanDeposit(slot.item);
      //   });
      //   if (snapshot == null) {
      //     continue;
      //   }
      //
      //   mem = snapshot;
      // }
      //
      // return mem;
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

      return new MoveToBestUnfinishedStrategy(agent, dest) {
        updateDestinationContinuously = updateDestinationContinuously,
        targetFromMemory = targetFromMemory
      };
    }
  }
}
