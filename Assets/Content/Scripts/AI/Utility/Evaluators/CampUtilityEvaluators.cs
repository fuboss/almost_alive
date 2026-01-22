using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;
using VContainer;

namespace Content.Scripts.AI.Utility {
  /// <summary>
  /// Returns value based on how much camp needs building.
  /// </summary>
  [Serializable]
  public class CampBuildingUtilityEvaluator : EvaluatorBase {
    [Inject] private RecipeModule _recipeModule;

    [Tooltip("Base value when camp needs building")]
    public float baseValue = 0.6f;

    public override float Evaluate(IGoapAgentCore agent) {
      if (agent is not ICampAgent campAgent) return 0f;
      if (agent is not IWorkAgent workAgent) return 0f;
      if (agent is not IInventoryAgent invAgent) return 0f;
      
      var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      if (camp?.setup == null) return 0f;
      if (camp.setup.allSpotsFilled) return 0f;

      if (_recipeModule == null) return 0f;
      var campRecipes = workAgent.recipes.GetUnlockedCampRecipes(_recipeModule);
      if (!campRecipes.Any(r => _recipeModule.CanCraft(r, invAgent.inventory))) return 0f;

      var emptyCount = camp.setup.GetEmptySpots().Count();
      var totalCount = camp.setup.spotCount;
      var emptyRatio = (float)emptyCount / totalCount;

      return baseValue * emptyRatio;
    }
  }
}
