using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [Serializable]
  public class VisionTagUtilityEvaluator : EvaluatorBase {
    public enum Mode {
      ANY_VISIBLE,
      COUNT,
      NEAREST_DISTANCE
    }

    public Mode mode = Mode.ANY_VISIBLE;
    [ValueDropdown("Tags")] public string[] tags;

    [ShowIf("mode", Mode.COUNT)] public int maxCount = 5;

    [ShowIf("mode", Mode.NEAREST_DISTANCE)]
    public float maxDistance = 20f;

    public AnimationCurve responseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override float Evaluate(IGoapAgentCore agent) {
      var sensor = agent.agentBrain.visionSensor;
      if (sensor == null) return 0f;

      var visible = sensor.ObjectsWithTagsInView(tags);

      switch (mode) {
        case Mode.ANY_VISIBLE: {
          var hasAny = sensor.HasObjectsWithTagsInView(tags);
          return responseCurve.Evaluate(hasAny ? 1f : 0f);
        }

        case Mode.COUNT: {
          var count = visible.Count();
          var t = Mathf.Clamp01((float)count / maxCount);
          return responseCurve.Evaluate(t);
        }

        case Mode.NEAREST_DISTANCE: {
          var nearestDist = float.MaxValue;
          foreach (var actor in visible) {
            var dist = Vector3.Distance(agent.position, actor.transform.position);
            if (dist < nearestDist) nearestDist = dist;
          }

          if (nearestDist >= float.MaxValue) return responseCurve.Evaluate(0f);
          var t = Mathf.Clamp01(nearestDist / maxDistance);
          return responseCurve.Evaluate(t);
        }

        default:
          return 0f;
      }
    }
  }
}
