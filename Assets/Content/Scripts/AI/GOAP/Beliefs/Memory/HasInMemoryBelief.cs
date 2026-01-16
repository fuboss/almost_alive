using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs.Memory {
  [Serializable]
  public class HasInMemoryBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;
    public bool checkDistance;
    public int minCount = 1;
    [EnableIf("checkDistance")] public float maxDistance = 20;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var memory = agent.memory;
        var withTags = memory.GetWithAllTags(tags);
        if (withTags.Length == 0) {
          return false;
        }

        if (!checkDistance) {
          return withTags.Length >= minCount;
        }

        return withTags.Count(m => Vector3.Distance(agent.position, m.location) < maxDistance) >= minCount;
      };
    }

    public override AgentBelief Copy() {
      var copy = new HasInMemoryBelief {
        tags = tags,
        checkDistance = checkDistance,
        maxDistance = maxDistance,
        name = name,
        condition = condition,
        minCount = minCount
      };
      return copy;
    }
  }
}