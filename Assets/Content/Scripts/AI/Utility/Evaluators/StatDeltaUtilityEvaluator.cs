using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class StatDeltaUtilityEvaluator : EvaluatorBase {
    public StatType statType;
    
    [Tooltip("Expected range of delta values for normalization")]
    public float minDelta = -1f;
    public float maxDelta = 1f;
    
    [Tooltip("If true, negative delta (stat decreasing) gives higher utility")]
    public bool prioritizeDecay = true;

    public AnimationCurve responseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override float Evaluate(IGoapAgent agent) {
      var body = agent.body;
      if (body == null) return 0f;

      if (!body.perTickDelta.TryGetValue(statType, out var delta))
        return 0f;

      // Normalize delta to 0-1 range
      var t = Mathf.InverseLerp(minDelta, maxDelta, delta);
      
      if (prioritizeDecay) t = 1f - t; // negative delta → higher value
      
      return responseCurve.Evaluate(t);
    }
  }
}