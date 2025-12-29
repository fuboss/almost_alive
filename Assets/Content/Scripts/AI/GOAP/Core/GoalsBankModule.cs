using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Core {
  public class GoalsBankModule {
    private List<GoalSO> _goalSOs;

    private GoalsBankModule(List<GoalSO> goalSOs) {
      _goalSOs = goalSOs;
      Debug.LogError($"GoalsBankModule:{_goalSOs?.Count.ToString() ?? "NULL"}");
    }

    public static GoalsBankModule GetFromResources() {
      var goalSOs = Resources.LoadAll<GoalSO>("GOAP/Goals").ToList();
      return new GoalsBankModule(goalSOs);
    }

    public IReadOnlyCollection<AgentGoal> GetAgentDefaultGoals(IGoapAgent agent) {
      return _goalSOs.Select(so => so.Get(agent)).ToList();
    }
  }
}