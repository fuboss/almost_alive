using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.Navigation;
using Content.Scripts.Game.Craft;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies.Move {
  [Serializable]
  public class MoveToRequiredResourceStrategy : MoveStrategy {
    public MoveToRequiredResourceStrategy(IGoapAgent agent, Func<Vector3> destination) : base(agent, destination) {
    }

    public MoveToRequiredResourceStrategy() {
    }

    protected override MemorySnapshot GetTargetMemory() {
      var camp = _agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      var unfinished = UnfinishedQuery.GetNeedingResources(camp);
      
      if (unfinished == null) {
        return targetFromMemory.GetNearest(_agent, s => s.HasTag(Tag.RESOURCE));
      }

      // Get all required resource tags
      var requiredTags = unfinished.GetRemainingResources().Select(n => n.tag).ToArray();
      
      // Collect all matching memories
      var candidates = requiredTags
        .SelectMany(tag => _agent.memory.GetWithAnyTags(new[] { tag }))
        .Distinct()
        .ToArray();

      if (candidates.Length == 0) return null;

      // Find nearest reachable
      return PathCostEvaluator.GetNearestReachable(_agent.navMeshAgent, candidates);
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

      return new MoveToRequiredResourceStrategy(agent, dest) {
        updateDestinationContinuously = updateDestinationContinuously,
        targetFromMemory = targetFromMemory
      };
    }
  }
}
