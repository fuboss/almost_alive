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
    public ActionPlan Plan(IGoapAgentCore agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null) {
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

    private ActionPlan BuildPlan(IGoapAgentCore agent, AgentGoal goal, StringBuilder b) {
      var requiredEffects = new HashSet<AgentBelief>(goal.desiredEffects);

      // Remove already satisfied effects
      requiredEffects.RemoveWhere(belief => belief.Evaluate(agent));
      if (requiredEffects.Count == 0) return null;

      var availableActions = new HashSet<AgentAction>(agent.agentBrain.actions);
      var plan = new List<AgentAction>();
      var totalCost = 0f;
      var totalBenefit = 0f;
      var visitedEffects = new HashSet<string>();

      // Backward chaining: find actions that satisfy required effects
      var stepIndex = 0;
      var debug = agent.agentBrain.debugPlanning;
      
      while (requiredEffects.Count > 0) {
        stepIndex++;
        
        var bestAction = FindBestAction(requiredEffects, availableActions, visitedEffects, b, debug);

        if (bestAction == null) {
          b.AppendLine($"[Step {stepIndex}] FAILED - no action for: {string.Join(", ", requiredEffects.Select(e => e.name))}");
          return null;
        }

        if (debug) {
          Debug.Log($"[GOAP {stepIndex}] {bestAction.name} → covers [{string.Join(", ", bestAction.effects.Where(e => requiredEffects.Any(r => r.name == e.name)).Select(e => e.name))}]");
        }

        plan.Add(bestAction);
        totalCost += bestAction.cost;
        totalBenefit += bestAction.benefit;
        availableActions.Remove(bestAction);

        foreach (var effect in bestAction.effects) {
          visitedEffects.Add(effect.name);
        }

        requiredEffects.ExceptWith(bestAction.effects);

        // Add preconditions that aren't already satisfied
        foreach (var pre in bestAction.preconditions) {
          if (!pre.Evaluate(agent) && !visitedEffects.Contains(pre.name)) {
            requiredEffects.Add(pre);
          }
        }
      }

      if (plan.Count == 0) {
        return null;
      }

      // Reverse to get execution order (we built it backwards)
      plan.Reverse();
      var actionStack = new Stack<AgentAction>(plan.AsEnumerable().Reverse());
      
      if (debug) {
        Debug.Log($"[GOAP] Plan: {string.Join(" → ", actionStack.Select(a => a.name))}");
      }
      
      var newPlan = new ActionPlan(goal, actionStack, totalCost, totalBenefit);
      return newPlan;
    }

    private AgentAction FindBestAction(
      HashSet<AgentBelief> requiredEffects,
      HashSet<AgentAction> availableActions,
      HashSet<string> visitedEffects,
      StringBuilder b,
      bool debug) {
      AgentAction best = null;
      var bestScore = float.MinValue;
      var candidates = new List<(AgentAction action, float score, int count, int dependencyPenalty)>();

      foreach (var action in availableActions) {
        var providesRequired = action.effects.Where(e =>
          requiredEffects.Any(r => r.name == e.name)).ToArray();
        var count = providesRequired.Length;
        if (count == 0) continue;

        var allEffectsVisited = action.effects.All(e => visitedEffects.Contains(e.name));
        if (allEffectsVisited) continue;

        // Penalty for actions that depend on effects already in visitedEffects
        // These actions need results from actions already in plan, so they should execute AFTER them
        // In backward chaining, "execute after" means "select earlier"
        // So we BOOST score for actions that have preconditions in visitedEffects
        var dependsOnVisited = action.preconditions.Count(p => visitedEffects.Contains(p.name));
        
        // Prioritize actions that cover MORE required effects (count^2 gives exponential weight)
        // Base score from action is secondary
        var score = count * count * 50 + action.score * 5 + dependsOnVisited * 100;
        candidates.Add((action, score, count, dependsOnVisited));
        
        if (score > bestScore) {
          bestScore = score;
          best = action;
        }
      }

      // Debug log candidates when multiple options exist
      if (debug && candidates.Count > 1) {
        var candidatesStr = string.Join(", ", candidates
          .OrderByDescending(c => c.score)
          .Select(c => $"{c.action.name}:{c.score:F0}"));
        Debug.Log($"[GOAP] Candidates: {candidatesStr}");
      }

      return best;
    }

    private static List<AgentGoal> GetOrderedGoals(IGoapAgentCore agent, HashSet<AgentGoal> goals, AgentGoal recentGoal) {
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

    public async UniTask<ActionPlan> PlanAsync(IGoapAgentCore agent, HashSet<AgentGoal> goals,
      AgentGoal mostRecentGoal = null) {
      await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
      return Plan(agent, goals, mostRecentGoal);
    }
  }
}
