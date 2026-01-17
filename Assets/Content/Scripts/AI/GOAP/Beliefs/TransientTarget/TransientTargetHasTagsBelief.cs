using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Craft;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs.TransientTarget {
  [Serializable, TypeInfoBox("True when transient target has all specified tags (or inverse: missing tags).")]
  public class TransientTargetHasTagsBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        if (tags == null) {
          Debug.LogError($"{GetType().Name} has null tags array");
          return false;
        }

        if (!inverse)
          return agent.transientTarget != null && agent.transientTarget.HasAllTags(tags);
        return agent.transientTarget == null || !agent.transientTarget.HasAllTags(tags);
      };
    }

    public override AgentBelief Copy() {
      return new TransientTargetHasTagsBelief {
        inverse = inverse,
        name = name,
        condition = condition,
        tags = tags
      };
    }
  }

  [Serializable, TypeInfoBox("True when transient target is required for crafting")]
  public class TransientIsCraftResourceBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var transient = agent.transientTarget;
        if (transient == null) return false;
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        var unfinishedTarget = UnfinishedQuery.GetNeedingResources(camp);
        if (unfinishedTarget == null) return false;

        var transientTags = transient.descriptionData.tags;
        var needs = unfinishedTarget.GetRemainingResources();
        var check = needs.Any(need => transientTags.Contains(need.tag));
        return !inverse ? check : !check;
      };
    }

    public override AgentBelief Copy() {
      return new TransientIsCraftResourceBelief {
        inverse = inverse,
        name = name,
      };
    }
  }
}