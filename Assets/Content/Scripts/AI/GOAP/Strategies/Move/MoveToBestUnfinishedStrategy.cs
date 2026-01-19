using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.Game.Craft;
using Content.Scripts.Game.Storage;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies.Move {
  [Serializable]
  public class MoveToBestUnfinishedStrategy : MoveStrategy {
    public MoveToBestUnfinishedStrategy(IGoapAgent agent, Func<Vector3> destination) : base(agent, destination) {
    }

    public MoveToBestUnfinishedStrategy() {
    }

    protected override MemorySnapshot GetTargetMemory() {
      var camp = _agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      var target = UnfinishedQuery.GetAllAtCamp(camp).FirstOrDefault();
      if (target != null) {
        if (_agent.memory.Recall(ms => ms.target.gameObject == target.gameObject, out var snapshot)) {
          return snapshot;
        }
      }
      else {
        Debug.LogError("MoveToBestUnfinishedStrategy: No unfinished tasks found at camp", camp);
      }

      var slotWithHaulableItem =
        _agent.inventory.occupiedSlots.Where(s => s.item.descriptionData.tags.Contains(Tag.ITEM)).ToArray();

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

    public override IActionStrategy Create(IGoapAgent agent) {
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