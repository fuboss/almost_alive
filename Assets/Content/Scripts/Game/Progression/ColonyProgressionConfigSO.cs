using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.Craft;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Progression {
  /// <summary>
  /// Configuration for colony progression - what's unlocked at each milestone.
  /// Milestones can be levels, research, events, etc.
  /// </summary>
  [CreateAssetMenu(fileName = "ColonyProgression", menuName = "Game/Colony Progression", order = 0)]
  public class ColonyProgressionConfigSO : SerializedScriptableObject {
    [Title("Recipe Unlocks")]
    [TableList]
    public List<RecipeUnlockEntry> recipeUnlocks = new();

    [Title("Starting Unlocks")]
    [Tooltip("Recipes available from the start (milestone = 0)")]
    public List<RecipeSO> starterRecipes = new();

    /// <summary>Get all recipes unlocked at or below given milestone.</summary>
    public List<RecipeSO> GetUnlockedRecipes(int milestone) {
      var result = new List<RecipeSO>(starterRecipes.Where(r => r != null));
      
      result.AddRange(recipeUnlocks
        .Where(u => u.recipe != null && u.milestone <= milestone)
        .Select(u => u.recipe));
      
      return result;
    }

    /// <summary>Get recipes unlocked exactly at this milestone.</summary>
    public List<RecipeSO> GetNewUnlocks(int milestone) {
      if (milestone == 0) return starterRecipes.Where(r => r != null).ToList();
      
      return recipeUnlocks
        .Where(u => u.recipe != null && u.milestone == milestone)
        .Select(u => u.recipe)
        .ToList();
    }

    [Serializable]
    public class RecipeUnlockEntry {
      [HorizontalGroup("Row"), LabelWidth(70)]
      [Tooltip("Milestone level required")]
      public int milestone = 1; //todo: change to smth more complex later(for research tree)
      
      [HorizontalGroup("Row"), LabelWidth(50)]
      public RecipeSO recipe;
      
      [LabelWidth(80)]
      [Tooltip("Optional: requires specific research")]
      public string requiresResearch;
    }

#if UNITY_EDITOR
    [Button("Sort by Milestone"), PropertyOrder(-1)]
    private void SortByMilestone() {
      recipeUnlocks = recipeUnlocks
        .OrderBy(u => u.milestone)
        .ThenBy(u => u.recipe?.name)
        .ToList();
    }

    [Button("Load All Recipes"), PropertyOrder(-1)]
    private void LoadAllRecipes() {
      var allRecipes = UnityEditor.AssetDatabase.FindAssets("t:RecipeSO")
        .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
        .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeSO>)
        .Where(r => r != null)
        .ToList();

      foreach (var recipe in allRecipes) {
        if (starterRecipes.Contains(recipe)) continue;
        if (recipeUnlocks.Any(u => u.recipe == recipe)) continue;
        
        starterRecipes.Add(recipe);
      }
      
      UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
  }
}
