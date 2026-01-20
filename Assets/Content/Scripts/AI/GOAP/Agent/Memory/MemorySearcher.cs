using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.Navigation;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityUtils;

namespace Content.Scripts.AI.GOAP.Agent.Memory {
  [Serializable]
  public class MemorySearcher {
    public SearchMode searchMode = SearchMode.NEAREST;
    
    [Tooltip("Use NavMesh path distance instead of Euclidean. More accurate but slower.")]
    public bool useNavMeshDistance = true;
    
    [Tooltip("Skip unreachable targets when using NavMesh distance.")]
    [ShowIf("useNavMeshDistance")]
    public bool filterUnreachable = true;
    
    [ValueDropdown("GetTags")] 
    public string[] requiredTags;

#if UNITY_EDITOR
    public List<string> GetTags() {
      return GOAPEditorHelper.GetTags();
    }
#endif

    public MemorySnapshot Search(IGoapAgent agent, Func<MemorySnapshot, bool> predicate = null) {
      var targetMem = searchMode switch {
        SearchMode.NEAREST => GetNearest(agent, predicate),
        SearchMode.ANY => agent.memory.GetWithAllTags(requiredTags).Random(),
        _ => GetNearest(agent, predicate)
      };

      if (targetMem != null) {
        Debug.Log($"SearchResult: {targetMem.target.name} {targetMem.location}", targetMem.target);
        return targetMem;
      }

      Debug.LogError("MemorySearcher: No target found in memory!");
      return null;
    }

    public MemorySnapshot GetNearest(IGoapAgent agent, Func<MemorySnapshot, bool> predicate = null) {
      var candidates = agent.memory.GetWithAllTags(requiredTags);
      
      if (candidates.Length == 0) return null;

      // Apply custom predicate filter
      if (predicate != null) {
        candidates = candidates.Where(ms => ms.target != null && predicate(ms)).ToArray();
      } else {
        candidates = candidates.Where(ms => ms.target != null).ToArray();
      }

      if (candidates.Length == 0) return null;

      // Single candidate - skip sorting
      if (candidates.Length == 1) {
        var single = candidates[0];
        if (useNavMeshDistance && filterUnreachable) {
          if (!PathCostEvaluator.IsReachable(agent.navMeshAgent, single.location)) {
            return null;
          }
        }
        return single;
      }

      // Sort by distance (NavMesh or Euclidean)
      if (useNavMeshDistance) {
        return GetNearestByNavMesh(agent, candidates);
      }
      
      return GetNearestByEuclidean(agent.position, candidates);
    }

    private MemorySnapshot GetNearestByNavMesh(IGoapAgent agent, MemorySnapshot[] candidates) {
      MemorySnapshot best = null;
      var bestCost = float.MaxValue;

      foreach (var ms in candidates) {
        var cost = PathCostEvaluator.GetPathCost(agent.navMeshAgent, ms.location);
        
        // Skip unreachable
        if (filterUnreachable && cost >= float.MaxValue) continue;
        
        if (cost < bestCost) {
          bestCost = cost;
          best = ms;
        }
      }

      return best;
    }

    private MemorySnapshot GetNearestByEuclidean(Vector3 position, MemorySnapshot[] candidates) {
      MemorySnapshot best = null;
      var bestDist = float.MaxValue;

      foreach (var ms in candidates) {
        var dist = (ms.location - position).sqrMagnitude;
        if (dist < bestDist) {
          bestDist = dist;
          best = ms;
        }
      }

      return best;
    }

    public enum SearchMode {
      NEAREST,
      FARTHEST,
      ANY
    }
  }
}
