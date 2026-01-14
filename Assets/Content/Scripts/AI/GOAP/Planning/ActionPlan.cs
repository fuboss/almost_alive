using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Goals;

namespace Content.Scripts.AI.GOAP.Planning {
  public class ActionPlan {
    public ActionPlan(AgentGoal goal, Stack<AgentAction> actions, float totalCost) {
      agentGoal = goal;
      this.actions = actions;
      this.totalCost = totalCost;
      totalActions = actions.Count;
      initialCost = totalCost;
    }

    public AgentGoal agentGoal { get; }
    public Stack<AgentAction> actions { get; }
    public float totalCost { get; set; }

    public int totalActions { get; }
    public int completedActions { get; private set; }
    public float initialCost { get; }

    public float commitment => totalActions > 0
      ? (float)completedActions / totalActions
      : 0f;

    public void MarkActionComplete() {
      completedActions++;
    }
  }
}