using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Beliefs.Memory;
using Content.Scripts.Game;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  [Serializable]
  public class HasNoHaulableItemInMemoryBelief : HasNoInMemoryBelief {
    public override bool Evaluate(IGoapAgent agent) {
      condition = () => {
        var memory = agent.memory;
        var withTags = memory.GetWithAllTags(tags)
          .Where(s => s.target.GetComponent<ActorDescription>().canPickup)
          .ToArray();
        
        if (withTags.Length == 0) {
          Debug.LogWarning($"HasInMemoryBelief: No items found with tags: '{string.Join(", ", tags)}'");
        }

        return withTags.Length > 0;
      };

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy() {
      return new HasHaulableItemInMemoryBelief() {
        tags = tags,
        name = name
      };
    }
  }
}