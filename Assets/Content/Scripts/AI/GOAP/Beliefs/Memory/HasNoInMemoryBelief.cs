using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Memory {
  [Serializable, TypeInfoBox("True when agent does not remember enough objects with specified tags.")]
  public class HasNoInMemoryBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;
    public int minCount = 1;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var withTags = agent.memory.GetWithAllTags(tags);
        return withTags.Length < minCount;
      };
    }

    public override AgentBelief Copy() {
      return new HasNoInMemoryBelief {
        tags = tags,
        name = name,
        condition = condition,
        minCount = minCount
      };
    }
  }
}
