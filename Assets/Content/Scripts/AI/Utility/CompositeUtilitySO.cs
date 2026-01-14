using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  [CreateAssetMenu(menuName = "GOAP/Utility/Composite")]
  public class CompositeUtilitySO : UtilitySO {
    public enum Mode {
      MULTIPLY,
      MIN,
      MAX
    }

    public Mode mode;

    public List<UtilitySO> utilities = new();

    public override float Evaluate(IGoapAgent agent) {
      if (utilities.Count == 0) {
        return 1f;
      }

      switch (mode) {
        case Mode.MIN:
          return utilities.Min(u => u.Evaluate(agent));

        case Mode.MAX:
          return utilities.Max(u => u.Evaluate(agent));

        case Mode.MULTIPLY:
        default:
          var value = 1f;
          foreach (var u in utilities) {
            value *= u.Evaluate(agent);
          }

          return value;
      }
    }

    public override IUtilityEvaluator CopyEvaluator() {
      return MemberwiseClone() as IUtilityEvaluator;
    }
  }
}