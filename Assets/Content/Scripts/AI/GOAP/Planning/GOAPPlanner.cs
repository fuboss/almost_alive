using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Planning {
  public class GOAPPlanner : IGoapPlanner {
    public ActionPlan Plan(IGoapAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null) {
      var orderedGoals = GetOrderedGoals(agent, goals, mostRecentGoal);

      foreach (var goal in orderedGoals) {
        var plan = BuildPlan(agent, goal);
        if (plan != null) {
          return plan;
        }
      }

      Debug.LogWarning("No plan found");
      return null;
    }

    private ActionPlan BuildPlan(IGoapAgent agent, AgentGoal goal) {
      var requiredEffects = new HashSet<AgentBelief>(goal.desiredEffects);

      // Remove already satisfied effects
      requiredEffects.RemoveWhere(b => b.Evaluate(agent));
      if (requiredEffects.Count == 0) return null;

      var availableActions = new HashSet<AgentAction>(agent.agentBrain.actions);
      var plan = new List<AgentAction>();
      var totalCost = 0f;
      var totalBenefit = 0f;
      var visitedEffects = new HashSet<string>();

      // Backward chaining: find actions that satisfy required effects
      while (requiredEffects.Count > 0) {
        var bestAction = FindBestAction(requiredEffects, availableActions, visitedEffects);

        if (bestAction == null) {
          // No action can satisfy remaining effects
          return null;
        }

        plan.Add(bestAction);
        totalCost += bestAction.cost;
        totalBenefit += bestAction.benefit;
        availableActions.Remove(bestAction);

        // Mark effects as visited
        foreach (var effect in bestAction.effects) {
          visitedEffects.Add(effect.name);
        }

        // Update required effects
        requiredEffects.ExceptWith(bestAction.effects);

        // Add preconditions that aren't already satisfied
        foreach (var pre in bestAction.preconditions) {
          if (!pre.Evaluate(agent) && !visitedEffects.Contains(pre.name)) {
            requiredEffects.Add(pre);
          }
        }
      }

      if (plan.Count == 0) return null;

      // Reverse to get execution order (we built it backwards)
      plan.Reverse();
      var actionStack = new Stack<AgentAction>(plan.AsEnumerable().Reverse());

      var newPlan = new ActionPlan(goal, actionStack, totalCost, totalBenefit);

      Debug.Log(
        $"ActionPlan for goal: <b>{goal.Name}</b>, cost: {totalCost:F1}, benefit: {totalBenefit:F1}, score: {newPlan.Score:F2}. " +
        $"{string.Join(" → ", plan.Select(a => $"[{a.name}]"))}");

      return newPlan;
    }

    /// <summary>
    /// Find best action that satisfies at least one required effect.
    /// Prioritizes by score (benefit/cost).
    /// </summary>
    private AgentAction FindBestAction(
      HashSet<AgentBelief> requiredEffects,
      HashSet<AgentAction> availableActions,
      HashSet<string> visitedEffects) {
      AgentAction best = null;
      var bestScore = float.MinValue;

      var b = new StringBuilder($"[{string.Join(", ", requiredEffects.Select(e => e.name))}]  Checking actions:\n");
      foreach (var action in availableActions) {
        // Check if action provides any required effect
        var providesRequired = action.effects.Where(e =>
          requiredEffects.Any(r => r.name == e.name)).ToArray();
        var count = providesRequired.Length;
        if (count == 0) continue;

        // Skip if all effects are already visited (avoid redundant actions)
        var allEffectsVisited = action.effects.All(e => visitedEffects.Contains(e.name));
        if (allEffectsVisited) continue;

        var score = action.score * count * 10;
        if (score > bestScore) {
          bestScore = score;
          best = action;
        }

        b.AppendLine(
          $" - Action: <b>{action.name}</b>, provides: {count}, score: {score:F2} (best: {best?.name}{bestScore:F2})");
      }

      Debug.Log($"FindBestAction:{b}");
      return best;
    }

    private static List<AgentGoal>
      GetOrderedGoals(IGoapAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal) {
      return goals
        .Where(g => g != null && g.desiredEffects.Any(b => {
          if (b == null) {
            Debug.LogError($"goal '{g.Name}' has a null desired effect!");
            return false;
          }

          try {
            return !b.Evaluate(agent);
          }
          catch (Exception e) {
            Debug.LogException(e, agent.agentBrain);
          }

          return false;
        }))
        .OrderByDescending(g => g.Name == mostRecentGoal?.Name ? g.Priority - 2 : g.Priority)
        .ToList();
    }

    public async UniTask<ActionPlan> PlanAsync(IGoapAgent agent, HashSet<AgentGoal> goals,
      AgentGoal mostRecentGoal = null) {
      await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
      return Plan(agent, goals, mostRecentGoal);
    }
  }
}