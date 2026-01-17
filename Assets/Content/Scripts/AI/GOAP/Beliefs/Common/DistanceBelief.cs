using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs.Common {
  [Serializable, TypeInfoBox("True when agent is within range of specified location (or inverse: outside range).")]
  public class DistanceBelief : AgentBelief {
    public Vector3 location;
    public float range = 2;
    public bool inverted;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var distance = Vector3.Distance(agent.position, location);
        return !inverted ? distance < range : distance >= range;
      };
    }

    public override AgentBelief Copy() {
      return new DistanceBelief {
        location = location,
        range = range,
        inverted = inverted,
        name = name,
        condition = condition
      };
    }
  }
}
