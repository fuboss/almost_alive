using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Descriptors;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class TransientTargetHasTagsBelief : AgentBelief {
    public string[] tags;
    public bool inverse = false;

    public override bool Evaluate(IGoapAgent agent) {
      _condition = () => {
        if (!inverse)
          return agent.transientTarget != null && agent.transientTarget.GetComponent<ActorDescription>().HasAllTags(tags);
        return agent.transientTarget == null || !agent.transientTarget.GetComponent<ActorDescription>().HasAllTags(tags);
      };

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