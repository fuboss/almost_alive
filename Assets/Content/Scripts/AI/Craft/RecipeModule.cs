using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.Craft {
  public class RecipeModule : IInitializable, IDisposable {
    [Inject] private ActorCreationModule _actorCreation;
    
    private readonly List<RecipeSO> _recipes = new();
    private readonly Dictionary<string, RecipeSO> _byResultKey = new();
    private readonly Dictionary<CraftStationType, List<RecipeSO>> _byStation = new();

    void IInitializable.Initialize() {
      var loaded = Resources.LoadAll<RecipeSO>("Recipes");
      _recipes.AddRange(loaded);

      foreach (var recipe in _recipes) {
        _byResultKey[recipe.recipe.resultActorKey] = recipe;

        if (!_byStation.TryGetValue(recipe.recipe.stationType, out var list)) {
          list = new List<RecipeSO>();
          _byStation[recipe.recipe.stationType] = list;
        }
        list.Add(recipe);
      }

      Debug.Log($"[RecipeModule] Loaded {_recipes.Count} recipes");
    }

    public string[] GetResultActorTags(RecipeSO recipe) {
      var prefab = _actorCreation.GetPrefab(recipe.recipe.resultActorKey);
      return prefab.descriptionData.tags;
    }
    
    public IReadOnlyList<RecipeSO> all => _recipes;

    /// <summary>Get recipe by result actor key.</summary>
    public RecipeSO GetByResult(string actorKey) {
      return _byResultKey.GetValueOrDefault(actorKey);
    }

    /// <summary>Get all recipes for a specific station type.</summary>
    public IReadOnlyList<RecipeSO> GetByStation(CraftStationType station) {
      return _byStation.TryGetValue(station, out var list) ? list : Array.Empty<RecipeSO>();
    }

    /// <summary>Get all recipes that can be crafted by hand.</summary>
    public IReadOnlyList<RecipeSO> GetHandCraftable() => GetByStation(CraftStationType.None);

    /// <summary>Check if agent has all required resources in inventory.</summary>
    public bool CanCraft(RecipeSO recipe, ActorInventory inventory) {
      if (recipe?.recipe == null || inventory == null) return false;

      foreach (var requiredResource in recipe.recipe.requiredResources) {
        if (inventory.GetItemCount(requiredResource.tag) < requiredResource.count) return false;
      }
      return true;
    }

    /// <summary>Find all recipes agent can currently craft.</summary>
    public List<RecipeSO> GetCraftable(ActorInventory inventory, CraftStationType? stationFilter = null) {
      var result = new List<RecipeSO>();
      var source = stationFilter.HasValue ? GetByStation(stationFilter.Value) : _recipes;

      foreach (var recipe in source) {
        if (CanCraft(recipe, inventory)) result.Add(recipe);
      }
      return result;
    }

    void IDisposable.Dispose() {
      _recipes.Clear();
      _byResultKey.Clear();
      _byStation.Clear();
    }

    
  }
}
