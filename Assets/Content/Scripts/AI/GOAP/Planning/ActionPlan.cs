using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Goals;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Planning {
  public class ActionPlan {
    public ActionPlan(AgentGoal goal, Stack<AgentAction> actions, float totalCost, float totalBenefit = 0f) {
      agentGoal = goal;
      this.actions = actions;
      this.totalCost = totalCost;
      this.totalBenefit = totalBenefit;
      totalActions = actions.Count;
      initialCost = totalCost;
    }

    public AgentGoal agentGoal { get; }
    public Stack<AgentAction> actions { get; }
    public float totalCost { get; set; }
    public float totalBenefit { get; }
    
    /// <summary>
    /// Plan score. Higher = better plan.
    /// </summary>
    public float Score => totalBenefit / Mathf.Max(totalCost, 0.1f);

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
