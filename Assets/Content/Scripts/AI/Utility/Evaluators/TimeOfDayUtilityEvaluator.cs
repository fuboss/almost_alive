using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Core.Simulation;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class TimeOfDayUtilityEvaluator : EvaluatorBase {
    [Tooltip("Length of one day in simulation seconds")]
    public float dayLengthSeconds = 1440f; // 24 min real-time at 1x = 1 day
    
    [Tooltip("Curve sampled at normalized time of day (0 = midnight, 0.5 = noon, 1 = midnight)")]
    public AnimationCurve responseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override float Evaluate(IGoapAgent agent) {
      var simTime = SimulationTimeController.instance;
      if (simTime == null) return responseCurve.Evaluate(0.5f);
      
      var normalizedTime = (simTime.totalSimTime % dayLengthSeconds) / dayLengthSeconds;
      return responseCurve.Evaluate(normalizedTime);
    }
  }
}