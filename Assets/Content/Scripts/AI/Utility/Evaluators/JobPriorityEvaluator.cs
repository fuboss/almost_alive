using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Work;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class JobPriorityEvaluator : EvaluatorBase {
    public WorkType workType;
    public AnimationCurve evaluationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public override float Evaluate(IGoapAgent agent) {
      var jobPriority = agent.GetWorkScheduler().GetPriority(workType);
      if (jobPriority == WorkPriority.DISABLED) return 0f;
      var t = Mathf.InverseLerp(WorkPriority.MIN_PRIORITY, WorkPriority.MAX_PRIORITY, jobPriority);
      var result = evaluationCurve.Evaluate(t);
      return result;
    }
  }
}