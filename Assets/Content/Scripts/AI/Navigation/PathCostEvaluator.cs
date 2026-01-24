using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent.Memory;
using UnityEngine;
using UnityEngine.AI;

namespace Content.Scripts.AI.Navigation {
  /// <summary>
  /// Evaluates NavMesh path cost for realistic "nearest" calculations.
  /// </summary>
  public static class PathCostEvaluator {
    private static readonly NavMeshPath _sharedPath = new();

    /// <summary>
    /// Get NavMesh path cost (total distance) to target.
    /// Returns float.MaxValue if unreachable.
    /// </summary>
    //todo: pretty heavy method. Improve perfomance
    public static float GetPathCost(NavMeshAgent agent, Vector3 target) {
      if (agent == null) return float.MaxValue;

      // Sample target position to NavMesh
      //todo: use specified area mask!
      if (!NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas)) {
        return float.MaxValue;
      }

      if (!agent.CalculatePath(hit.position, _sharedPath)) {
        return float.MaxValue;
      }

      if (_sharedPath.status != NavMeshPathStatus.PathComplete) {
        return float.MaxValue;
      }

      // Sum path segment lengths
      var cost = 0f;
      var corners = _sharedPath.corners;
      for (var i = 1; i < corners.Length; i++) {
        cost += Vector3.Distance(corners[i - 1], corners[i]);
      }

      return cost;
    }

    /// <summary>
    /// Check if target is reachable via complete NavMesh path.
    /// </summary>
    public static bool IsReachable(NavMeshAgent agent, Vector3 target) {
      if (agent == null) return false;

      if (!NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas)) {
        return false;
      }

      if (!agent.CalculatePath(hit.position, _sharedPath)) {
        return false;
      }

      return _sharedPath.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// Find nearest reachable MemorySnapshot by NavMesh path cost.
    /// </summary>
    public static MemorySnapshot GetNearestReachable(
      NavMeshAgent agent,
      MemorySnapshot[] candidates,
      Func<MemorySnapshot, bool> predicate = null) {
      
      if (candidates == null || candidates.Length == 0) return null;

      MemorySnapshot best = null;
      var bestCost = float.MaxValue;

      foreach (var ms in candidates) {
        if (ms?.target == null) continue;
        if (predicate != null && !predicate(ms)) continue;

        var cost = GetPathCost(agent, ms.location);
        if (cost >= float.MaxValue) continue; // unreachable

        if (cost < bestCost) {
          bestCost = cost;
          best = ms;
        }
      }

      return best;
    }

    /// <summary>
    /// Find nearest reachable from list, with Euclidean fallback if all unreachable.
    /// </summary>
    public static MemorySnapshot GetNearestPreferReachable(
      NavMeshAgent agent,
      Vector3 agentPosition,
      MemorySnapshot[] candidates,
      Func<MemorySnapshot, bool> predicate = null) {
      
      // Try NavMesh first
      var reachable = GetNearestReachable(agent, candidates, predicate);
      if (reachable != null) return reachable;

      // Fallback to Euclidean
      MemorySnapshot best = null;
      var bestDist = float.MaxValue;

      foreach (var ms in candidates) {
        if (ms?.target == null) continue;
        if (predicate != null && !predicate(ms)) continue;

        var dist = (ms.location - agentPosition).sqrMagnitude;
        if (dist < bestDist) {
          bestDist = dist;
          best = ms;
        }
      }

      return best;
    }
  }
}
