using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Content.Scripts.Game.Storage;
using VContainer;

namespace Content.Scripts.AI.GOAP.Beliefs.Craft {
  /// <summary>True when camp has active unfinished actor.</summary>
  [Serializable]
  public class HasActiveUnfinishedBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return UnfinishedQuery.HasActiveUnfinished(camp);
      };
    }

    public override AgentBelief Copy() => new HasActiveUnfinishedBelief { name = name };
  }

  /// <summary>True when unfinished needs resources.</summary>
  [Serializable]
  public class UnfinishedNeedsResourcesBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return UnfinishedQuery.GetNeedingResources(camp) != null;
      };
    }

    public override AgentBelief Copy() => new UnfinishedNeedsResourcesBelief { name = name };
  }

  /// <summary>True when unfinished has all resources and needs work.</summary>
  [Serializable]
  public class UnfinishedNeedsWorkBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return UnfinishedQuery.GetNeedingWork(camp) != null;
      };
    }

    public override AgentBelief Copy() => new UnfinishedNeedsWorkBelief { name = name };
  }

  /// <summary>True when agent can deliver resources (has them or can get from storage).</summary>
  [Serializable]
  public class CanDeliverToUnfinishedBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        var target = UnfinishedQuery.GetNeedingResources(camp);
        if (target == null) return false;

        var needs = target.GetRemainingResources();
        foreach (var (tag, _) in needs) {
          if (agent.inventory.GetItemCount(tag) > 0) return true;
          if (HasResourceInCampStorages(camp, tag)) return true;
        }

        return false;
      };
    }

    private bool HasResourceInCampStorages(CampLocation camp, string tag) {
      if (camp == null) return false;
      var campPos = camp.transform.position;
      
      return ActorRegistry<StorageActor>.all
        .Any(s => UnityEngine.Vector3.Distance(s.transform.position, campPos) < 30f && 
                  s.GetCountWithTag(tag) > 0);
    }

    public override AgentBelief Copy() => new CanDeliverToUnfinishedBelief { name = name };
  }

  /// <summary>True when camp needs new unfinished (empty spots and unlocked recipes).</summary>
  [Serializable]
  public class CampNeedsNewUnfinishedBelief : AgentBelief {
    [Inject] private RecipeModule _recipeModule;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        if (camp?.setup == null) return false;
        
        if (UnfinishedQuery.HasActiveUnfinished(camp)) return false;
        if (camp.setup.allSpotsFilled) return false;
        if (_recipeModule == null) return false;
        
        var campRecipes = agent.recipes.GetUnlockedCampRecipes(_recipeModule);
        
        foreach (var recipe in campRecipes) {
          var tags = _recipeModule.GetResultActorTags(recipe);
          var tag = tags?.Length > 0 ? tags[0] : null;
          
          if (!string.IsNullOrEmpty(tag)) {
            if (camp.setup.GetSpotsNeedingTag(tag).Any()) return true;
          } else {
            if (camp.setup.GetAnyEmptySpot() != null) return true;
          }
        }
        
        return false;
      };
    }

    public override AgentBelief Copy() => new CampNeedsNewUnfinishedBelief { 
      name = name, 
      _recipeModule = _recipeModule 
    };
  }
}
