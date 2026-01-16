using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Agent;
using VContainer;

namespace Content.Scripts.AI.GOAP.Beliefs.Camp {
  [Serializable]
  public class HasCampBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return camp != null && camp.hasSetup;
      };
    }

    public override AgentBelief Copy() => new HasCampBelief { name = name };
  }

  /// <summary>Inverse of HasCampBelief - true when agent needs a camp.</summary>
  [Serializable]
  public class NeedsCampBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return camp == null || !camp.hasSetup;
      };
    }

    public override AgentBelief Copy() => new NeedsCampBelief { name = name };
  }

  /// <summary>True when camp has empty spots and agent can build something.</summary>
  [Serializable]
  public class CampNeedsBuildingBelief : AgentBelief {
    [Inject] private RecipeModule _recipeModule;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        if (camp?.setup == null) return false;
        if (camp.setup.allSpotsFilled) return false;
        
        // Check if agent can build any unlocked camp recipe
        if (_recipeModule == null) return false;
        var campRecipes = agent.recipes.GetUnlockedCampRecipes(_recipeModule);
        return campRecipes.Any(r => _recipeModule.CanCraft(r, agent.inventory));
      };
    }

    public override AgentBelief Copy() => new CampNeedsBuildingBelief { name = name, _recipeModule = _recipeModule };
  }

  /// <summary>True when camp is fully built (all spots filled).</summary>
  [Serializable]
  public class CampFullyBuiltBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return camp?.setup != null && camp.setup.allSpotsFilled;
      };
    }

    public override AgentBelief Copy() => new CampFullyBuiltBelief { name = name };
  }
}
