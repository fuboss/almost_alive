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
      var goalsStr = string.Join(",", orderedGoals.Select(g => $"[{g.Name}:{g.Priority}]"));
     

      var maxScore = float.MinValue;
      ActionPlan plan = null;
      var goalB = new StringBuilder();
      foreach (var goal in orderedGoals) {
        var b = new StringBuilder();
        var potentialPlan = BuildPlan(agent, goal, b);
        if (potentialPlan == null) {
          goalB.AppendLine($"Plan for {goal.Name} failed to build:\n{b}");
          continue;
        }

        if (potentialPlan.Score > maxScore) {
          plan = potentialPlan;
          maxScore = potentialPlan.Score;
        }

        goalB.AppendLine($"plan for {goal.Name} has score {potentialPlan.Score:F2}" +
                         $" {string.Join(" → ", potentialPlan.actions.Select(a => $"[{a.name}]"))}");
      }

      if (plan != null) {
        Debug.Log(
          $"[GOAPPlanner] Selected plan '<b>{plan.agentGoal.Name}</b>' {plan.actionNames}\n for" +
          $"{goalsStr}\n\n {goalB}");
        return plan;
      }

      Debug.LogWarning("No plan found");
      return null;
    }

    private ActionPlan BuildPlan(IGoapAgent agent, AgentGoal goal, StringBuilder b) {
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
        var bestAction = FindBestAction(requiredEffects, availableActions, visitedEffects, b);

        if (bestAction == null) { b.AppendLine(
            $" - <b>[Failed to find action to resolve effects: {string.Join(",", requiredEffects.Select(e => e.name))}; </b>]\n");
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
        b.Append($"→ <b>{bestAction.name}</b>[{string.Join(",", bestAction.effects.Select(e => e.name))}] ");
      }

      if (plan.Count == 0) {
        b.AppendLine(
          $" - Failed to build Plan. notCovered effects: {string.Join("", $"[{requiredEffects.Select(e => e.name)}]")}; ");
        return null;
      }

      // Reverse to get execution order (we built it backwards)
      plan.Reverse();
      var actionStack = new Stack<AgentAction>(plan.AsEnumerable().Reverse());
      var newPlan = new ActionPlan(goal, actionStack, totalCost, totalBenefit);
      return newPlan;
    }

    /// <summary>
    /// Find best action that satisfies at least one required effect.
    /// Prioritizes by score (benefit/cost).
    /// </summary>
    private AgentAction FindBestAction(
      HashSet<AgentBelief> requiredEffects,
      HashSet<AgentAction> availableActions,
      HashSet<string> visitedEffects,
      StringBuilder b) {
      AgentAction best = null;
      var bestScore = float.MinValue;

      //b.AppendLine($"{string.Join(", ", requiredEffects.Select(e => e.name))}]  Checking actions:\n");
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
      }
      
      return best;
    }

    private static List<AgentGoal> GetOrderedGoals(IGoapAgent agent, HashSet<AgentGoal> goals, AgentGoal recentGoal) {
      var desires = goals.Select(g => new GoalDesires(g)).ToList();
      foreach (var desire in desires) {
        foreach (var belief in desire.checks.Keys.ToList()) {
          desire.checks[belief] = !belief.Evaluate(agent);
        }
      }

      return desires.Where(d => d.checks.Any(kvp => kvp.Value)).Select(d => d.goal)
        .OrderByDescending(g => g.Name == recentGoal?.Name ? g.Priority - 0.5f : g.Priority)
        .ToList();
    }

    private class GoalDesires {
      public AgentGoal goal;
      public Dictionary<AgentBelief, bool> checks = new();

      public GoalDesires(AgentGoal g) {
        goal = g;
        foreach (var belief in g.desiredEffects) {
          checks[belief] = false;
        }
      }
    }


    public async UniTask<ActionPlan> PlanAsync(IGoapAgent agent, HashSet<AgentGoal> goals,
      AgentGoal mostRecentGoal = null) {
      await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
      return Plan(agent, goals, mostRecentGoal);
    }
  }
}