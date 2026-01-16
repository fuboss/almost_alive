using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Construction;
using Content.Scripts.Game.Storage;
using VContainer;

namespace Content.Scripts.AI.GOAP.Beliefs.Construction {
  /// <summary>True when camp has active construction site.</summary>
  [Serializable]
  public class HasActiveConstructionBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return ConstructionQuery.HasActiveConstruction(camp);
      };
    }

    public override AgentBelief Copy() => new HasActiveConstructionBelief { name = name };
  }

  /// <summary>True when construction site needs resources.</summary>
  [Serializable]
  public class ConstructionNeedsResourcesBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        var site = ConstructionQuery.GetNeedingResources(camp);
        return site != null;
      };
    }

    public override AgentBelief Copy() => new ConstructionNeedsResourcesBelief { name = name };
  }

  /// <summary>True when construction site has all resources and needs work.</summary>
  [Serializable]
  public class ConstructionNeedsWorkBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        var site = ConstructionQuery.GetNeedingWork(camp);
        return site != null;
      };
    }

    public override AgentBelief Copy() => new ConstructionNeedsWorkBelief { name = name };
  }

  /// <summary>True when agent can deliver resources to construction (has them or can get from storage).</summary>
  [Serializable]
  public class CanDeliverToConstructionBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        var site = ConstructionQuery.GetNeedingResources(camp);
        if (site == null) return false;

        var needs = site.GetRemainingResources();
        foreach (var (tag, _) in needs) {
          // Check agent inventory
          if (agent.inventory.GetItemCount(tag) > 0) return true;
          
          // Check camp storages
          if (HasResourceInCampStorages(camp, tag)) return true;
        }

        return false;
      };
    }

    private bool HasResourceInCampStorages(CampLocation camp, string tag) {
      if (camp == null) return false;
      var campPos = camp.transform.position;
      
      return ActorRegistry<StorageActor>.all
        .Any(s => Vector3.Distance(s.transform.position, campPos) < 30f && 
                  s.GetCountWithTag(tag) > 0);
    }

    public override AgentBelief Copy() => new CanDeliverToConstructionBelief { name = name };
  }

  /// <summary>True when camp needs new construction (empty spots and unlocked recipes).</summary>
  [Serializable]
  public class CampNeedsNewConstructionBelief : AgentBelief {
    [Inject] private RecipeModule _recipeModule;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        if (camp?.setup == null) return false;
        
        // Skip if already has active construction
        if (ConstructionQuery.HasActiveConstruction(camp)) return false;
        
        // Check for empty spots
        if (camp.setup.allSpotsFilled) return false;
        
        // Check if agent has any unlocked camp recipes
        if (_recipeModule == null) return false;
        var campRecipes = agent.recipes.GetUnlockedCampRecipes(_recipeModule);
        
        // Find recipe that has available spot
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

    public override AgentBelief Copy() => new CampNeedsNewConstructionBelief { 
      name = name, 
      _recipeModule = _recipeModule 
    };
  }
}
