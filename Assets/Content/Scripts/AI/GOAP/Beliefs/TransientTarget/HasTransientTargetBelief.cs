using System;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs.TransientTarget {
  [Serializable]
  public class HasTransientTargetBelief : AgentBelief {
    public bool inverse = false;

    public override bool Evaluate(IGoapAgent agent) {
      condition = () => !inverse 
        ? agent.transientTarget != null
        : agent.transientTarget == null;

      return base.Evaluate(agent);
    }
    
    public override AgentBelief Copy() {
      var copy = new HasTransientTargetBelief {
        inverse = inverse,
        name = name,
        condition = condition
      };
      return copy;
    }
  }
}