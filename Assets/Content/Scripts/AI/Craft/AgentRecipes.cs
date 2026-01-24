using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Craft {
  [Serializable]
  public class AgentRecipes {
    [SerializeField] private RecipeProgressionSO _progression;
    [ShowInInspector, ReadOnly] private HashSet<string> _unlockedRecipeIds = new();

    public RecipeProgressionSO progression => _progression;
    public IReadOnlyCollection<string> unlockedRecipeIds => _unlockedRecipeIds;

    public void Initialize(int level) {
      RefreshUnlocks(level);
    }

    public void RefreshUnlocks(int level) {
      if (_progression == null) return;
      
      _unlockedRecipeIds.Clear();
      foreach (var recipe in _progression.GetUnlockedRecipes(level)) {
        _unlockedRecipeIds.Add(recipe.recipeId);
      }
    }

    public void OnLevelUp(int newLevel) {
      if (_progression == null) return;
      
      var newRecipes = _progression.GetNewUnlocks(newLevel);
      foreach (var recipe in newRecipes) {
        _unlockedRecipeIds.Add(recipe.recipeId);
        Debug.Log($"[AgentRecipes] Unlocked: {recipe.recipeId}");
      }
    }

    public bool IsUnlocked(RecipeSO recipe) {
      return recipe != null && _unlockedRecipeIds.Contains(recipe.recipeId);
    }

    public bool IsUnlocked(string recipeId) {
      return _unlockedRecipeIds.Contains(recipeId);
    }

    /// <summary>Get all unlocked camp recipes sorted by priority (highest first).</summary>
    public List<RecipeSO> GetUnlockedRecipes(RecipeModule recipeModule) {
      return recipeModule.all
        .Where(IsUnlocked)
        .OrderByDescending(r => r.buildPriority)
        .ToList();
    }
  }
}
