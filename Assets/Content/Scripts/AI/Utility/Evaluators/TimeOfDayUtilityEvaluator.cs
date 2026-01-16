using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Core.Environment;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class TimeOfDayUtilityEvaluator : EvaluatorBase {
    [Tooltip("Curve sampled at normalized time of day (0 = midnight, 0.5 = noon, 1 = midnight)")]
    public AnimationCurve responseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override float Evaluate(IGoapAgent agent) {
      var env = WorldEnvironment.instance;
      if (env == null) return responseCurve.Evaluate(0.5f);
      return responseCurve.Evaluate(env.dayCycle.normalizedTime);
    }
  }
}
