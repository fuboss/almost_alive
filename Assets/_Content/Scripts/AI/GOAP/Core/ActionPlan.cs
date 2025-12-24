using System.Collections.Generic;

namespace _Content.Scripts.AI.GOAP.Core {
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