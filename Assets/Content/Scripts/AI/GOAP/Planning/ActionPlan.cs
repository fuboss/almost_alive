using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Goals;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Planning {
  public class ActionPlan {
    public ActionPlan(AgentGoal goal, Stack<AgentAction> actions, float totalCost) {
      AgentGoal = goal;
      Actions = actions;
      TotalCost = totalCost;
    }

    public AgentGoal AgentGoal { get; }
    public Stack<AgentAction> Actions { get; }
    public float TotalCost { get; set; }
  }
}