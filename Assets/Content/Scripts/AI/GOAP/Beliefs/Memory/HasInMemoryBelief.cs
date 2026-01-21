using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs.Memory {
  [Serializable, TypeInfoBox("True when agent remembers object with specified tags (or inverse: doesn't remember). Optionally checks distance.")]
  public class HasInMemoryBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;
    public bool checkDistance;
    public int minCount = 1;
    [EnableIf("checkDistance")] public float maxDistance = 20;
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      var sqrMaxDistance = maxDistance * maxDistance;
      return () => {
        var memory = agent.memory;
        
        if (!checkDistance) {
          if (minCount == 1) {
            var has = memory.HasWithAllTags(tags);
            return !inverse ? has : !has;
          }
          var count = memory.CountWithAllTags(tags);
          var hasEnough = count >= minCount;
          return !inverse ? hasEnough : !hasEnough;
        }
        
        var withTags = memory.GetWithAllTags(tags);
        if (withTags.Length == 0) {
          return inverse;
        }

        var foundCount = 0;
        var agentPos = agent.position;
        foreach (var m in withTags) {
          if ((agentPos - m.location).sqrMagnitude < sqrMaxDistance) {
            foundCount++;
            if (foundCount >= minCount) break;
          }
        }
        var hasEnoughInRange = foundCount >= minCount;
        return !inverse ? hasEnoughInRange : !hasEnoughInRange;
      };
    }

    public override AgentBelief Copy() {
      return new HasInMemoryBelief {
        tags = tags,
        checkDistance = checkDistance,
        maxDistance = maxDistance,
        name = name,
        minCount = minCount,
        inverse = inverse
      };
    }
  }
}
