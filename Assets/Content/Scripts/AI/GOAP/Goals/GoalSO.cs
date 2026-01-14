using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Goals {
  [System.Serializable]
  public class GoalTemplate {
    public string name;
    [ValueDropdown("GetEffectNames")] public List<string> desiredEffects = new();

    [Title("Utility")] public List<GoalUtility> utilityEvaluators = new();
    [MinValue(0f)] public float utilityBias = 1f;

    public AgentGoal Get(IGoapAgent agent) {
      float utility = EvaluateUtility(agent);
      var builder = new AgentGoal.Builder(name)
        .WithDesiredEffects(desiredEffects.Select(agent.GetBelief))
        .WithPriority(utility);

      return builder.Build();
    }

    private float EvaluateUtility(IGoapAgent agent) {
      var value = utilityBias;

      foreach (var evaluator in utilityEvaluators) {
        value *= evaluator.evaluator.Evaluate(agent);
      }

      return value;
    }

#if UNITY_EDITOR
    public List<string> GetEffectNames() => GOAPEditorHelper.GetBeliefsNames();
#endif
  }

  [CreateAssetMenu(fileName = "Goal", menuName = "GOAP/Goal", order = 0)]
  public class GoalSO : SerializedScriptableObject {
    public GoalTemplate template = new GoalTemplate();

    private void OnValidate() {
      template.name = name;
    }
  }
}