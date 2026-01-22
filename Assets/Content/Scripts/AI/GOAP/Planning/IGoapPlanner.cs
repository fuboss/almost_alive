using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Goals;
using Cysharp.Threading.Tasks;

namespace Content.Scripts.AI.GOAP.Planning {
  public interface IGoapPlanner {
    ActionPlan Plan(IGoapAgentCore agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null);
    UniTask<ActionPlan> PlanAsync(IGoapAgentCore agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null);
  }
}
