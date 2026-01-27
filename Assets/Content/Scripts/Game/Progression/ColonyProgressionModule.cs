using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.Craft;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Game.Progression {
  /// <summary>
  /// Central service for colony progression.
  /// Tracks unlocked recipes, technologies, milestones.
  /// </summary>
  public class ColonyProgressionModule : IStartable {
    [Inject] private ColonyProgressionConfigSO _config;
    [Inject] private RecipeModule _recipeModule;

    private int _currentMilestone;
    private readonly HashSet<string> _unlockedRecipeIds = new();
    private readonly HashSet<string> _completedResearch = new();

    /// <summary>Current progression milestone (like colony level).</summary>
    public int currentMilestone => _currentMilestone;

    /// <summary>All unlocked recipe IDs.</summary>
    public IReadOnlyCollection<string> unlockedRecipeIds => _unlockedRecipeIds;

    /// <summary>Fired when new recipes are unlocked.</summary>
    public event Action<IReadOnlyList<RecipeSO>> OnRecipesUnlocked;

    /// <summary>Fired when milestone changes.</summary>
    public event Action<int> OnMilestoneChanged;

    public void Start() {
      // Initialize with milestone 0 (starter unlocks)
      SetMilestone(0);
    }

    /// <summary>Set current milestone and unlock everything up to it.</summary>
    public void SetMilestone(int milestone) {
      if (_config == null) {
        Debug.LogWarning("[ColonyProgression] Config not assigned!");
        return;
      }

      _currentMilestone = milestone;
      RefreshUnlocks();
      OnMilestoneChanged?.Invoke(_currentMilestone);
    }

    /// <summary>Advance to next milestone.</summary>
    public void AdvanceMilestone() {
      var previousUnlocks = _unlockedRecipeIds.Count;
      _currentMilestone++;
      
      var newRecipes = _config.GetNewUnlocks(_currentMilestone);
      foreach (var recipe in newRecipes) {
        if (CanUnlock(recipe)) {
          _unlockedRecipeIds.Add(recipe.recipeId);
        }
      }

      if (_unlockedRecipeIds.Count > previousUnlocks) {
        OnRecipesUnlocked?.Invoke(newRecipes);
      }

      OnMilestoneChanged?.Invoke(_currentMilestone);
      Debug.Log($"[ColonyProgression] Advanced to milestone {_currentMilestone}, unlocked: {newRecipes.Count} recipes");
    }

    /// <summary>Complete a research and unlock associated recipes.</summary>
    public void CompleteResearch(string researchId) {
      if (_completedResearch.Contains(researchId)) return;
      
      _completedResearch.Add(researchId);
      RefreshUnlocks();
      
      Debug.Log($"[ColonyProgression] Research completed: {researchId}");
    }

    /// <summary>Check if recipe is unlocked.</summary>
    public bool IsUnlocked(RecipeSO recipe) {
      return recipe != null && _unlockedRecipeIds.Contains(recipe.recipeId);
    }

    /// <summary>Check if recipe is unlocked by ID.</summary>
    public bool IsUnlocked(string recipeId) {
      return _unlockedRecipeIds.Contains(recipeId);
    }

    /// <summary>Get all unlocked recipes.</summary>
    public List<RecipeSO> GetUnlockedRecipes() {
      return _recipeModule.all
        .Where(IsUnlocked)
        .OrderByDescending(r => r.buildPriority)
        .ToList();
    }

    /// <summary>Get unlocked recipes filtered by category tag.</summary>
    public List<RecipeSO> GetUnlockedRecipes(string categoryTag) {
      return _recipeModule.all
        .Where(r => IsUnlocked(r) && HasTag(r, categoryTag))
        .OrderByDescending(r => r.buildPriority)
        .ToList();
    }

    private void RefreshUnlocks() {
      _unlockedRecipeIds.Clear();
      
      var allUnlocks = _config.GetUnlockedRecipes(_currentMilestone);
      foreach (var recipe in allUnlocks) {
        if (CanUnlock(recipe)) {
          _unlockedRecipeIds.Add(recipe.recipeId);
        }
      }
    }

    private bool CanUnlock(RecipeSO recipe) {
      // Find unlock entry for this recipe
      var entry = _config.recipeUnlocks.FirstOrDefault(u => u.recipe == recipe);
      
      // Starter recipes always unlockable
      if (entry == null) return true;
      
      // Check research requirement
      if (!string.IsNullOrEmpty(entry.requiresResearch)) {
        if (!_completedResearch.Contains(entry.requiresResearch)) {
          return false;
        }
      }
      
      return true;
    }

    private bool HasTag(RecipeSO recipe, string tag) {
      // TODO: Add tags to RecipeSO or check result actor tags
      return true; // For now, return all
    }
  }
}
