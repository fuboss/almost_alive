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
        var check = GetCraftingNeeds(agent, transient);
        return !inverse ? check : !check;
      };
    }

    private static bool GetCraftingNeeds(IGoapAgent agent, ActorDescription transient) {
      //locate unfinished camp build target
      var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      var unfinishedTarget = UnfinishedQuery.GetNeedingResources(camp);
      if (unfinishedTarget != null) {
        var needs = unfinishedTarget.GetRemainingResources();
        var check = transient.HasAnyTags(needs.Select(n => n.tag).ToArray());
        return check;
      }

      //look in available resources for any crafting recipes needing this transient
      string[] resourcesTags = agent.recipeModule.GetResourcesTagsForAvailableRecipes(agent);
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