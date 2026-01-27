using System.Collections.Generic;
using Content.Scripts.AI.Craft;
using Content.Scripts.Building.Services;
using Content.Scripts.Game.Progression;
using Content.Scripts.Ui.Layers.BottomBar;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Ui.Commands {
  /// <summary>
  /// Registers build commands from unlocked recipes.
  /// Groups by recipe.category for submenu hierarchy.
  /// Updates when progression changes.
  /// </summary>
  public class BuildCommandsRegistrar : IStartable, ITickable {
    [Inject] private ColonyProgressionModule _progression;
    [Inject] private RecipeModule _recipeModule;
    [Inject] private ModulePlacementService _modulePlacement;

    private int _lastUnlockedCount;
    private bool _initialized;

    public void Start() {
      _progression.OnRecipesUnlocked += OnRecipesUnlocked;
      _progression.OnMilestoneChanged += OnMilestoneChanged;
    }

    public void Tick() {
      // Lazy init — wait for RecipeModule to load
      if (!_initialized && _recipeModule.all.Count > 0) {
        _initialized = true;
        RefreshBuildCommands();
      }
    }

    private void OnRecipesUnlocked(IReadOnlyList<RecipeSO> newRecipes) {
      RefreshBuildCommands();
    }

    private void OnMilestoneChanged(int milestone) {
      RefreshBuildCommands();
    }

    private void RefreshBuildCommands() {
      ClearBuildCommands();

      var unlocked = _progression.GetUnlockedRecipes();
      int order = 0;

      foreach (var recipe in unlocked) {
        RegisterBuildCommand(recipe, order++);
      }

      if (unlocked.Count > 0) {
        Debug.Log($"[BuildCommands] Registered {unlocked.Count} build commands");
      }
    }

    private void RegisterBuildCommand(RecipeSO recipe, int order) {
      var capturedRecipe = recipe;
      
      // Hierarchical label: "Category/DisplayName"
      var category = string.IsNullOrEmpty(recipe.category) ? "Other" : recipe.category;
      var displayName = recipe.GetDisplayName();
      var label = $"{category}/{displayName}";
      
      CommandRegistry.Register(new Command(
        id: $"build.{recipe.recipeId}",
        label: label,
        icon: "🏗️",
        category: CommandCategory.Build,
        execute: () => StartPlacement(capturedRecipe),
        canExecute: () => CanBuild(capturedRecipe),
        tooltip: GetRecipeTooltip(capturedRecipe),
        order: order
      ));
    }

    private void StartPlacement(RecipeSO recipe) {
      Debug.Log($"[Build] Start placement: {recipe.recipeId}");
      
      // TODO: Integration with placement system
      // Check if this recipe produces a module and start module placement
      // _modulePlacement.StartPlacement(moduleDefFromRecipe);
    }

    private bool CanBuild(RecipeSO recipe) {
      // TODO: Check resources availability
      return true;
    }

    private string GetRecipeTooltip(RecipeSO recipe) {
      var data = recipe.recipe;
      if (data.requiredResources.Count == 0) {
        return $"{recipe.GetDisplayName()}\nNo resources required";
      }
      
      var resources = string.Join(", ", 
        data.requiredResources.ConvertAll(r => $"{r.tag} x{r.count}"));
      
      return $"{recipe.GetDisplayName()}\nResources: {resources}\nWork: {data.workRequired}";
    }

    private void ClearBuildCommands() {
      var commands = new List<ICommand>(CommandRegistry.GetByCategory(CommandCategory.Build));
      foreach (var cmd in commands) {
        CommandRegistry.Unregister(cmd.id);
      }
    }
  }
}
