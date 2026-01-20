using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.TransientTarget {
  [Serializable, TypeInfoBox("True when agent has transient target set (or inverse: no target).")]
  public class HasTransientTargetBelief : AgentBelief {
    public bool inverse;
    
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => !inverse 
        ? agent.transientTarget != null
        : agent.transientTarget == null;
    }

    public override AgentBelief Copy() {
      return new HasTransientTargetBelief {
        inverse = inverse,
        name = name
      };
    }
  }
}
