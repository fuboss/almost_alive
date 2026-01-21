using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs.TransientTarget {
  [Serializable, TypeInfoBox("True when transient target has all specified tags (or inverse: missing tags).")]
  public class TransientTargetHasTagsBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      if (agent is not ITransientTargetAgent targetAgent) {
        return () => inverse;
      }
      
      return () => {
        if (tags == null) {
          Debug.LogError($"{GetType().Name} has null tags array");
          return false;
        }

        if (!inverse)
          return targetAgent.transientTarget != null && targetAgent.transientTarget.HasAllTags(tags);
        return targetAgent.transientTarget == null || !targetAgent.transientTarget.HasAllTags(tags);
      };
    }

    public override AgentBelief Copy() {
      return new TransientTargetHasTagsBelief {
        inverse = inverse,
        name = name,
        tags = tags
      };
    }
  }

  [Serializable, TypeInfoBox("True when transient target is required for crafting")]
  public class TransientIsCraftResourceBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      // Requires multiple interfaces
      if (agent is not ITransientTargetAgent targetAgent) return () => inverse;
      if (agent is not ICampAgent campAgent) return () => inverse;
      if (agent is not IWorkAgent workAgent) return () => inverse;
      
      return () => {
        var transient = targetAgent.transientTarget;
        if (transient == null) return inverse;
        var check = GetCraftingNeeds(agent, campAgent, workAgent, transient);
        return !inverse ? check : !check;
      };
    }

    private static bool GetCraftingNeeds(IGoapAgentCore agent, ICampAgent campAgent, IWorkAgent workAgent, ActorDescription transient) {
      var camp = campAgent.camp;
      var unfinishedTarget = UnfinishedQuery.GetNeedingResources(camp);
      if (unfinishedTarget != null) {
        var needs = unfinishedTarget.GetRemainingResources();
        var check = transient.HasAnyTags(needs.Select(n => n.tag).ToArray());
        return check;
      }

      string[] resourcesTags = workAgent.recipeModule.GetResourcesTagsForAvailableRecipes(workAgent);
      return transient.HasAnyTags(resourcesTags);
    }

    public override AgentBelief Copy() {
      return new TransientIsCraftResourceBelief {
        inverse = inverse,
        name = name,
      };
    }
  }
}
