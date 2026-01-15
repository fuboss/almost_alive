using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Beliefs.Memory;
using Content.Scripts.Game;
using Content.Scripts.Game.Decay;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs.Storage {
  [Serializable]
  public class HasHaulableItemInMemoryBelief : HasInMemoryBelief {
    public override bool Evaluate(IGoapAgent agent) {
      condition = () => {
        var memory = agent.memory;
        var withTags = memory.GetWithAllTags(tags)
          .Where(s => s.target.GetComponent<ActorDescription>().canPickup)
          .ToArray();
        
        if (withTags.Length == 0) {
          Debug.LogWarning($"HasInMemoryBelief: No items found with tags: '{string.Join(", ", tags)}'");
        }

        if (!checkDistance) {
          return withTags.Length > 0;
        }

        return withTags.Any(m => Vector3.Distance(agent.position, m.location) < maxDistance);
      };

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy() {
      return new HasHaulableItemInMemoryBelief() {
        tags = tags,
        maxDistance = maxDistance,
        name = name
      };
    }
  }
}