using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.Craft;
using Content.Scripts.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Progression {
  
  /// <summary>
  /// Entry for recipe unlock at specific milestone.
  /// </summary>
  [Serializable]
  public class RecipeUnlockEntry {
    [HorizontalGroup("Row"), LabelWidth(70)]
    [Tooltip("Milestone level required")]
    public int milestone = 1;
    
    [HorizontalGroup("Row"), LabelWidth(50)]
    public RecipeSO recipe;
    
    [LabelWidth(80)]
    [Tooltip("Optional: requires specific research")]
    public string requiresResearch;
  }

  /// <summary>
  /// Configuration data for colony progression - what's unlocked at each milestone.
  /// </summary>
  [Serializable]
  public class ColonyProgressionConfig {
    [Title("Recipe Unlocks")]
    [TableList]
    public List<RecipeUnlockEntry> recipeUnlocks = new();

    [Title("Starting Unlocks")]
    [Tooltip("Recipes available from the start (milestone = 0)")]
    public List<RecipeSO> starterRecipes = new();
  }

  /// <summary>
  /// ScriptableObject container for ColonyProgressionConfig.
  /// </summary>
  [CreateAssetMenu(fileName = "ColonyProgression", menuName = "Game/Colony Progression", order = 0)]
  public class ColonyProgressionConfigSO : ScriptableConfig<ColonyProgressionConfig> {

    /// <summary>Get all recipes unlocked at or below given milestone.</summary>
    public List<RecipeSO> GetUnlockedRecipes(int milestone) {
      var result = new List<RecipeSO>(Data.starterRecipes.Where(r => r != null));
      
      result.AddRange(Data.recipeUnlocks
        .Where(u => u.recipe != null && u.milestone <= milestone)
        .Select(u => u.recipe));
      
      return result;
    }

    /// <summary>Get recipes unlocked exactly at this milestone.</summary>
    public List<RecipeSO> GetNewUnlocks(int milestone) {
      if (milestone == 0) return Data.starterRecipes.Where(r => r != null).ToList();
      
      return Data.recipeUnlocks
        .Where(u => u.recipe != null && u.milestone == milestone)
        .Select(u => u.recipe)
        .ToList();
    }

#if UNITY_EDITOR
    [Button("Sort by Milestone"), PropertyOrder(-1)]
    private void SortByMilestone() {
      Data.recipeUnlocks = Data.recipeUnlocks
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
        if (Data.starterRecipes.Contains(recipe)) continue;
        if (Data.recipeUnlocks.Any(u => u.recipe == recipe)) continue;
        
        Data.starterRecipes.Add(recipe);
      }
      
      UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
  }
}
