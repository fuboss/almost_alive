using System;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs.TransientTarget {
  [Serializable]
  public class HasTransientTargetBelief : AgentBelief {
    public bool inverse = false;
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => !inverse 
        ? agent.transientTarget != null
        : agent.transientTarget == null;
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