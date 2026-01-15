using System;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class TransientTargetDistanceEvaluator : EvaluatorBase {
    [Tooltip("Returns this value when no transient target")]
    public float noTargetValue = 0f;
    
    public float maxDistance = 20f;
    
    [Tooltip("If true, closer = higher value. If false, farther = higher value")]
    public bool invertDistance = false;

    public AnimationCurve responseCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    public override float Evaluate(IGoapAgent agent) {
      var target = agent.transientTarget;
      if (target == null) return noTargetValue;

      var dist = Vector3.Distance(agent.position, target.transform.position);
      var t = Mathf.Clamp01(dist / maxDistance);
      
      if (invertDistance) t = 1f - t;
      
      return responseCurve.Evaluate(t);
    }
  }
}