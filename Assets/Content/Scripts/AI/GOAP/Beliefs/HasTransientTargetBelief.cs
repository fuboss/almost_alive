using System;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class HasTransientTargetBelief : AgentBelief {
    public bool inverse = false;

    public override bool Evaluate(IGoapAgent agent) {
      _condition = () => !inverse 
        ? agent.transientTarget != null
        : agent.transientTarget == null;

      return base.Evaluate(agent);
    }
    
    public override AgentBelief Copy(IGoapAgent agent) {
      var copy = new HasTransientTargetBelief {
        inverse = inverse,
        name = name,
        _condition = _condition
      };
      return copy;
    }
  }
}