using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Craft {
  [CreateAssetMenu(fileName = "RecipeProgression", menuName = "GOAP/RecipeProgression", order = 0)]
  public class RecipeProgressionSO : SerializedScriptableObject {
    [Serializable]
    public class RecipeUnlock {
      [HorizontalGroup("Row"), LabelWidth(50)]
      public int level = 1;
      
      [HorizontalGroup("Row"), LabelWidth(60)]
      public RecipeSO recipe;
    }

    [TableList]
    public List<RecipeUnlock> unlocks = new();

    /// <summary>Get all recipes unlocked at or below given level.</summary>
    public List<RecipeSO> GetUnlockedRecipes(int level) {
      return unlocks
        .Where(u => u.recipe != null && u.level <= level)
        .Select(u => u.recipe)
        .ToList();
    }

    /// <summary>Get recipes unlocked exactly at this level.</summary>
    public List<RecipeSO> GetNewUnlocks(int level) {
      return unlocks
        .Where(u => u.recipe != null && u.level == level)
        .Select(u => u.recipe)
        .ToList();
    }

    /// <summary>Check if specific recipe is unlocked at level.</summary>
    public bool IsUnlocked(RecipeSO recipe, int level) {
      return unlocks.Any(u => u.recipe == recipe && u.level <= level);
    }

#if UNITY_EDITOR
    [Button("Sort by Level")]
    private void SortByLevel() {
      unlocks = unlocks.OrderBy(u => u.level).ThenBy(u => u.recipe?.name).ToList();
    }

    [Button("Load All Recipes")]
    private void LoadAllRecipes() {
      var allRecipes = UnityEditor.AssetDatabase.FindAssets("t:RecipeSO", new[] { "Assets/Content/Resources/Recipes" })
        .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
        .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeSO>)
        .Where(r => r != null)
        .ToList();

      foreach (var recipe in allRecipes) {
        if (unlocks.Any(u => u.recipe == recipe)) continue;
        unlocks.Add(new RecipeUnlock { level = 1, recipe = recipe });
      }
      
      UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
  }
}
