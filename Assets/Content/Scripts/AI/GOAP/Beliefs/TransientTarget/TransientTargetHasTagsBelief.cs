using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.TransientTarget {
  [Serializable]
  public class TransientTargetHasTagsBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;
    public bool inverse = false;

    public override bool Evaluate(IGoapAgent agent) {
      condition = () => {
        if (!inverse)
          return agent.transientTarget != null && agent.transientTarget.GetComponent<ActorDescription>().HasAllTags(tags);
        return agent.transientTarget == null || !agent.transientTarget.GetComponent<ActorDescription>().HasAllTags(tags);
      };

      return base.Evaluate(agent);
    }
    
    public override AgentBelief Copy() {
      var copy = new TransientTargetHasTagsBelief {
        inverse = inverse,
        name = name,
        condition = condition,
        tags = tags
      };
      return copy;
    }
  }
}