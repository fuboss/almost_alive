using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Core;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Planning {
  public class ActionPlan {
    public ActionPlan(AgentGoal goal, Stack<AgentAction> actions, float totalCost) {
      AgentGoal = goal;
      Actions = actions;
      TotalCost = totalCost;
      Debug.Log($"ActionPlan created with goal: {goal.Name}, totalCost: {totalCost}");
    }

    public AgentGoal AgentGoal { get; }
    public Stack<AgentAction> Actions { get; }
    public float TotalCost { get; set; }
  }
}