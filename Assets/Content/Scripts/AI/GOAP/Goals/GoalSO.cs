using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Goals {

  [Serializable][HideReferenceObjectPicker][InlineEditor]
  public class GoalUtility {
    
    [OnValueChanged("ResetEvaluator")]
    [SerializeField] public UtilitySO utility;
    [ShowIf("@utility != null")]
    [SerializeField] public IUtilityEvaluator evaluator;

    private void ResetEvaluator() {
      if (utility == null) {
        evaluator = null;
        return;
      }
      
    }
  }
  
  [CreateAssetMenu(fileName = "Goal", menuName = "GOAP/Goal", order = 0)]
  public class GoalSO : SerializedScriptableObject {
    [ValueDropdown("GetEffectNames")] public List<string> desiredEffects = new();

    [Title("Utility")] public List<IUtilityEvaluator> utilityEvaluators = new();
    public List<GoalUtility> utilityEvaluators1 = new();
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
        value *= evaluator?.Evaluate(agent) ?? 1f;
      }

      return value;
    }

#if UNITY_EDITOR
    public List<string> GetEffectNames() => GOAPEditorHelper.GetBeliefsNames();
#endif
  }
}