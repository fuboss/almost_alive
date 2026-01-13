using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Planning {
  public class GOAPPlanner : IGoapPlanner {
    public ActionPlan Plan(IGoapAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null) {
      // Order goals by priority, descending
      var orderedGoals = GetOrderedGoals(agent, goals, mostRecentGoal);

      // Try to solve each goal in order
      foreach (var goal in orderedGoals) {
        var goalNode = new PlannerNode(null, null, goal.desiredEffects, 0);

        // If we can find a path to the goal, return the plan
        if (!FindPath(goalNode, agent, agent.agentBrain.actions)) continue;

        // If the goalNode has no leaves and no action to perform try a different goal
        if (goalNode.IsLeafDead) continue;

        var actionStack = new Stack<AgentAction>();
        while (goalNode.Leaves.Count > 0) {
          var cheapestLeaf = goalNode.Leaves.OrderBy(leaf => leaf.Cost).First();
          goalNode = cheapestLeaf;
          actionStack.Push(cheapestLeaf.Action);
        }

        var newPlan = new ActionPlan(goal, actionStack, goalNode.Cost);
        Debug.Log(
          $"ActionPlan for goal: {goal.Name}, totalCost: {newPlan.TotalCost}. {string.Join(",", actionStack.Select(a=>a.name))} actions in plan.\n" +
          $"[Goals: {string.Join(",", orderedGoals.Select(g => $"{g.Name}:{g.Priority}"))}]");
        return newPlan;
      }

      Debug.LogWarning("No plan found");
      return null;
    }

    private static List<AgentGoal> GetOrderedGoals(IGoapAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal) {
      var orderedGoals = goals
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
        .OrderByDescending(g => g == mostRecentGoal ? g.Priority - 1.5 : g.Priority)
        .ToList();
      return orderedGoals;
    }

    public async UniTask<ActionPlan> PlanAsync(IGoapAgent agent, HashSet<AgentGoal> goals,
      AgentGoal mostRecentGoal = null) {
      //todo: this is a stub. replace with real async planning
      await UniTask.Delay(TimeSpan.FromSeconds(1f));
      return Plan(agent, goals, mostRecentGoal);
    }

    // TODO: Consider a more powerful search algorithm like A* or D*
    private static bool FindPath(PlannerNode parent, IGoapAgent agent, HashSet<AgentAction> actions) {
      // Order actions by cost, ascending
      var orderedActions = actions.OrderBy(a => a.cost).ToList();

      foreach (var action in orderedActions) {
        var requiredEffects = parent.RequiredEffects;

        // Remove any effects that evaluate to true, there is no action to take
        requiredEffects.RemoveWhere(b => b.Evaluate(agent));

        // If there are no required effects to fulfill, we have a plan
        if (requiredEffects.Count == 0) return true;

        if (!action.effects.Any(requiredEffects.Contains)) continue;
        // Create new required effects for the child node
        var newRequiredEffects = new HashSet<AgentBelief>(requiredEffects);
        newRequiredEffects.ExceptWith(action.effects);
        newRequiredEffects.UnionWith(action.preconditions);

        var newAvailableActions = new HashSet<AgentAction>(actions);
        newAvailableActions.Remove(action);

        var newNode = new PlannerNode(parent, action, newRequiredEffects, parent.Cost + action.cost);

        // Explore the new node recursively
        if (FindPath(newNode, agent, newAvailableActions)) {
          parent.Leaves.Add(newNode);
          newRequiredEffects.ExceptWith(newNode.Action.preconditions);
        }

        // If all effects at this depth have been satisfied, return true
        if (newRequiredEffects.Count == 0) return true;
      }

      return parent.Leaves.Count > 0;
    }
  }
}