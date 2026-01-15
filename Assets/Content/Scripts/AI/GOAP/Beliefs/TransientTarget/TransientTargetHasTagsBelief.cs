using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.TransientTarget {
  [Serializable]
  public class TransientTargetHasTagsBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;
    public bool inverse = false;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        if (!inverse)
          return agent.transientTarget != null && agent.transientTarget.HasAllTags(tags);
        return agent.transientTarget == null || !agent.transientTarget.HasAllTags(tags);
      };
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