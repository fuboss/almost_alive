using System;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class CooldownUtilityEvaluator : EvaluatorBase {
    [Tooltip("Action name to check cooldown for. Empty = check last action.")]
    public string actionName;
    
    [Tooltip("Cooldown duration in seconds")]
    public float cooldownDuration = 30f;
    
    [Tooltip("Value returned during cooldown (at t=0)")]
    public float cooldownValue = 0f;
    
    [Tooltip("Value returned when cooldown expired (at t=1)")]
    public float readyValue = 1f;

    public AnimationCurve responseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override float Evaluate(IGoapAgent agent) {
      var tracker = agent.agentBrain.actionHistory;
      if (tracker == null) return readyValue;

      var targetAction = string.IsNullOrEmpty(actionName) 
        ? tracker.lastActionName 
        : actionName;

      if (string.IsNullOrEmpty(targetAction)) return readyValue;

      var timeSince = tracker.GetTimeSinceLastExecution(targetAction);
      
      if (timeSince >= float.MaxValue) return readyValue; // never executed
      
      var t = Mathf.Clamp01(timeSince / cooldownDuration);
      var curveValue = responseCurve.Evaluate(t);
      
      return Mathf.Lerp(cooldownValue, readyValue, curveValue);
    }
  }
}