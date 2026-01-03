using System;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class DistanceBelief : AgentBelief {
    public Vector3 location;
    public float range = 2;
    public bool inverted;

    public override bool Evaluate(IGoapAgent agent) {
      _condition = () => {
        var distance = Vector3.Distance(agent.position, location);
        return !inverted
          ? distance < range
          : distance >= range;
      };

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy(IGoapAgent agent) {
      var copy = new DistanceBelief {
        location = location,
        range = range,
        inverted = inverted,
        name = name,
        _condition = _condition
      };
      return copy;
    }
  }
}