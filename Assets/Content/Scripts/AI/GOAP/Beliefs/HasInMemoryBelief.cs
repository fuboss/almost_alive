using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class HasInMemoryBelief : AgentBelief {
    public string[] tags;
    public bool checkDistance;
    [EnableIf("checkDistance")] public float maxDistance = 20;

    public override bool Evaluate(IGoapAgent agent) {
      _condition = () => {
        var memory = agent.memory;
        var withTags = memory.GetWithAllTags(tags);
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
    
    public override AgentBelief Copy(IGoapAgent agent) {
      var copy = new HasInMemoryBelief {
        tags = tags,
        checkDistance = checkDistance,
        maxDistance = maxDistance,
        name = name,
        _condition = _condition
      };
      return copy;
    }
  }
}