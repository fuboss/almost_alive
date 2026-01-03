using System;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class HasNoInMemoryBelief : AgentBelief {
    public string[] tags;

    public override bool Evaluate(IGoapAgent agent) {
      _condition = () => {
        var withTags = agent.memory.GetWithAllTags(tags);
        if (withTags.Length == 0) {
          Debug.LogWarning($"HasNoInMemoryBelief: No items found with tags: '{string.Join(", ", tags)}'");
        }
        return withTags.Length == 0;
      };

      return base.Evaluate(agent);
    }
    
    public override AgentBelief Copy(IGoapAgent agent) {
      var copy = new HasNoInMemoryBelief {
        tags = tags,
        name = name,
        _condition = _condition
      };
      return copy;
    }
  }
}