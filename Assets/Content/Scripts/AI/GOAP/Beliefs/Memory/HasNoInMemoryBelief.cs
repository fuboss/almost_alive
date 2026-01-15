using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs.Memory {
  [Serializable]
  public class HasNoInMemoryBelief : AgentBelief {
    [ValueDropdown("GetTags")]public string[] tags;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var withTags = agent.memory.GetWithAllTags(tags);
        return withTags.Length == 0;
      };
    }

    public override AgentBelief Copy() {
      var copy = new HasNoInMemoryBelief {
        tags = tags,
        name = name,
        condition = condition
      };
      return copy;
    }
  }
}