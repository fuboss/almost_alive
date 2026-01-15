using System;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs.Common {
  [Serializable]
  public class DistanceBelief : AgentBelief {
    public Vector3 location;
    public float range = 2;
    public bool inverted;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var distance = Vector3.Distance(agent.position, location);
        return !inverted
          ? distance < range
          : distance >= range;
      };
    }

    public override AgentBelief Copy() {
      var copy = new DistanceBelief {
        location = location,
        range = range,
        inverted = inverted,
        name = name,
        condition = condition
      };
      return copy;
    }
  }
}