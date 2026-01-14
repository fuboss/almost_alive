using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Planning.Incremental {
  public class IncrementalPlanner : IGoapPlanner {
    private readonly Dictionary<AgentGoal, PlanCache> _cache = new();

    private readonly GOAPPlanner _fallbackPlanner = new();
    private int _cacheHits;
    private int _cacheMisses;

    public int maxCacheSize = 5;

    public ActionPlan Plan(IGoapAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null) {
      var orderedGoals = GetOrderedGoals(agent, goals, mostRecentGoal);

      foreach (var goal in orderedGoals) {
        // Check cache first
        if (_cache.TryGetValue(goal, out var cached) && !cached.IsExpired()) {
          if (ValidateCachedPlan(cached, agent)) {
            _cacheHits++;
            Debug.Log($"[IncrementalPlanner] Cache hit for goal: {goal.Name}");
            return cached.plan;
          }
        }

        // Try incremental repair if we have a cached plan
        if (cached != null && !cached.IsExpired()) {
          var invalidated = GetInvalidatedBeliefs(cached, agent);
          if (invalidated.Count < 3) { // Threshold for repair vs rebuild
            var repaired = TryRepairPlan(cached.plan, invalidated, agent);
            if (repaired != null) {
              UpdateCache(goal, repaired, agent);
              Debug.Log($"[IncrementalPlanner] Repaired plan for goal: {goal.Name}");
              return repaired;
            }
          }
        }

        // Fallback to full planning
        _cacheMisses++;
        var newPlan = _fallbackPlanner.Plan(agent, new HashSet<AgentGoal> { goal }, mostRecentGoal);

        if (newPlan != null) {
          UpdateCache(goal, newPlan, agent);
          return newPlan;
        }
      }

      return null;
    }

    public async UniTask<ActionPlan> PlanAsync(IGoapAgent agent, HashSet<AgentGoal> goals,
      AgentGoal mostRecentGoal = null) {
      await UniTask.Yield();
      return Plan(agent, goals, mostRecentGoal);
    }

    private ActionPlan TryRepairPlan(ActionPlan originalPlan, HashSet<AgentBelief> invalidated,
      IGoapAgent agent) {
      var actions = new Stack<AgentAction>(originalPlan.actions.Reverse());
      var repairedActions = new Stack<AgentAction>();
      var currentCost = 0f;

      while (actions.Count > 0) {
        var action = actions.Pop();

        // Check if this action is affected by invalidated beliefs
        var isAffected = action.preconditions.Any(invalidated.Contains) ||
                         action.effects.Any(invalidated.Contains);

        if (!isAffected) {
          repairedActions.Push(action);
          currentCost += action.cost;
          continue;
        }

        // Try to find replacement action
        var replacement = FindReplacementAction(action, agent, invalidated);
        if (replacement != null) {
          repairedActions.Push(replacement);
          currentCost += replacement.cost;
        }
        else {
          // Can't repair, return null
          return null;
        }
      }

      if (repairedActions.Count == 0) return null;

      return new ActionPlan(originalPlan.agentGoal, repairedActions, currentCost);
    }

    private AgentAction FindReplacementAction(AgentAction failed, IGoapAgent agent,
      HashSet<AgentBelief> invalidated) {
      var availableActions = agent.agentBrain.actions
        .Where(a => a.effects.Intersect(failed.effects).Any())
        .Where(a => a.AreAllPreconditionsMet(agent))
        .OrderBy(a => a.cost)
        .ToList();

      return availableActions.FirstOrDefault();
    }

    private HashSet<AgentBelief> GetInvalidatedBeliefs(PlanCache cached, IGoapAgent agent) {
      var invalidated = new HashSet<AgentBelief>();

      foreach (var belief in cached.requiredBeliefs) {
        var current = belief.Evaluate(agent);
        if (current != cached.beliefStates.GetValueOrDefault(belief, false)) invalidated.Add(belief);
      }

      return invalidated;
    }

    private bool ValidateCachedPlan(PlanCache cached, IGoapAgent agent) {
      foreach (var belief in cached.requiredBeliefs) {
        var currentState = belief.Evaluate(agent);
        if (currentState != cached.beliefStates.GetValueOrDefault(belief, false)) return false;
      }

      return true;
    }

    private void UpdateCache(AgentGoal goal, ActionPlan plan, IGoapAgent agent) {
      // Collect all beliefs used in this plan
      var requiredBeliefs = new HashSet<AgentBelief>();
      var beliefStates = new Dictionary<AgentBelief, bool>();

      foreach (var action in plan.actions) {
        foreach (var precondition in action.preconditions) {
          requiredBeliefs.Add(precondition);
          beliefStates[precondition] = precondition.Evaluate(agent);
        }

        foreach (var effect in action.effects) {
          requiredBeliefs.Add(effect);
          beliefStates[effect] = effect.Evaluate(agent);
        }
      }

      _cache[goal] = new PlanCache {
        plan = plan,
        requiredBeliefs = requiredBeliefs,
        beliefStates = beliefStates,
        creationTime = Time.time
      };

      // Evict old entries
      if (_cache.Count > maxCacheSize) {
        var oldest = _cache.OrderBy(kvp => kvp.Value.creationTime).First();
        _cache.Remove(oldest.Key);
      }
    }

    private List<AgentGoal> GetOrderedGoals(IGoapAgent agent, HashSet<AgentGoal> goals,
      AgentGoal mostRecentGoal) {
      return goals
        .Where(g => g != null && g.desiredEffects.Any(b => {
          if (b == null) return false;
          try {
            return !b.Evaluate(agent);
          }
          catch (Exception e) {
            Debug.LogException(e, agent.agentBrain);
            return false;
          }
        }))
        .OrderByDescending(g => g == mostRecentGoal ? g.Priority - 1.5 : g.Priority)
        .ToList();
    }

    public PlannerStats GetStats() {
      return new PlannerStats {
        CacheHits = _cacheHits,
        CacheMisses = _cacheMisses,
        CacheSize = _cache.Count,
        HitRate = _cacheHits + _cacheMisses > 0
          ? (float)_cacheHits / (_cacheHits + _cacheMisses)
          : 0f
      };
    }
  }

  public class PlanCache {
    public Dictionary<AgentBelief, bool> beliefStates;
    public float creationTime;
    public ActionPlan plan;
    public HashSet<AgentBelief> requiredBeliefs;

    public bool IsExpired(float expirationTime = 10f) {
      return Time.time - creationTime > expirationTime;
    }
  }

  public struct PlannerStats {
    public int CacheHits;
    public int CacheMisses;
    public int CacheSize;
    public float HitRate;
  }
}