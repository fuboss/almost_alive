using System.Collections.Generic;
using System.Linq;
using _Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace _Content.Scripts.AI.GOAP.Core {
  public interface IGoapPlanner {
    ActionPlan Plan(GoapAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null);
  }

  public class GoapPlanner : IGoapPlanner {
    public ActionPlan Plan(GoapAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null) {
      // Order goals by priority, descending
      var orderedGoals = goals
        .Where(g => g.DesiredEffects.Any(b => !b.Evaluate()))
        .OrderByDescending(g => g == mostRecentGoal ? g.Priority - 0.01 : g.Priority)
        .ToList();

      // Try to solve each goal in order
      foreach (var goal in orderedGoals) {
        var goalNode = new PlannerNode(null, null, goal.DesiredEffects, 0);

        // If we can find a path to the goal, return the plan
        if (FindPath(goalNode, agent.actions)) {
          // If the goalNode has no leaves and no action to perform try a different goal
          if (goalNode.IsLeafDead) continue;

          var actionStack = new Stack<AgentAction>();
          while (goalNode.Leaves.Count > 0) {
            var cheapestLeaf = goalNode.Leaves.OrderBy(leaf => leaf.Cost).First();
            goalNode = cheapestLeaf;
            actionStack.Push(cheapestLeaf.Action);
          }

          return new ActionPlan(goal, actionStack, goalNode.Cost);
        }
      }

      Debug.LogWarning("No plan found");
      return null;
    }

    // TODO: Consider a more powerful search algorithm like A* or D*
    private static bool FindPath(PlannerNode parent, HashSet<AgentAction> actions) {
      // Order actions by cost, ascending
      var orderedActions = actions.OrderBy(a => a.Cost);

      foreach (var action in orderedActions) {
        var requiredEffects = parent.RequiredEffects;

        // Remove any effects that evaluate to true, there is no action to take
        requiredEffects.RemoveWhere(b => b.Evaluate());

        // If there are no required effects to fulfill, we have a plan
        if (requiredEffects.Count == 0) return true;

        if (action.Effects.Any(requiredEffects.Contains)) {
          var newRequiredEffects = new HashSet<AgentBelief>(requiredEffects);
          newRequiredEffects.ExceptWith(action.Effects);
          newRequiredEffects.UnionWith(action.Preconditions);

          var newAvailableActions = new HashSet<AgentAction>(actions);
          newAvailableActions.Remove(action);

          var newNode = new PlannerNode(parent, action, newRequiredEffects, parent.Cost + action.Cost);

          // Explore the new node recursively
          if (FindPath(newNode, newAvailableActions)) {
            parent.Leaves.Add(newNode);
            newRequiredEffects.ExceptWith(newNode.Action.Preconditions);
          }

          // If all effects at this depth have been satisfied, return true
          if (newRequiredEffects.Count == 0) return true;
        }
      }

      return parent.Leaves.Count > 0;
    }
  }
}