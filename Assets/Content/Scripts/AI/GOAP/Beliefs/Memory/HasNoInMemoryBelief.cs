using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs.Memory {
  [Serializable]
  public class HasNoInMemoryBelief : AgentBelief {
    [ValueDropdown("GetTags")]public string[] tags;

    public override bool Evaluate(IGoapAgent agent) {
      condition = () => {
        var withTags = agent.memory.GetWithAllTags(tags);
        if (withTags.Length == 0) {
          Debug.LogWarning($"HasNoInMemoryBelief: No items found with tags: '{string.Join(", ", tags)}'");
        }
        return withTags.Length == 0;
      };

      return base.Evaluate(agent);
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