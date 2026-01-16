using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using ImprovedTimers;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace Content.Scripts.AI.GOAP.Strategies.Camp {
  /// <summary>
  /// Builds highest-priority camp actor at an empty CampSpot.
  /// Selects recipe based on: unlocked, can craft, matching spot tag, highest priority.
  /// </summary>
  [Serializable]
  public class BuildAtCampStrategy : AgentStrategy {
    [Inject] private RecipeModule _recipeModule;
    [Inject] private ActorCreationModule _actorCreation;

    private IGoapAgent _agent;
    private CampLocation _camp;
    private CampSpot _targetSpot;
    private RecipeSO _selectedRecipe;
    private CountdownTimer _buildTimer;
    private BuildState _state;

    public BuildAtCampStrategy() {
    }

    private BuildAtCampStrategy(IGoapAgent agent, BuildAtCampStrategy template) {
      _agent = agent;
      _recipeModule = template._recipeModule;
      _actorCreation = template._actorCreation;
    }

    public override bool canPerform => _recipeModule != null && _actorCreation != null;
    public override bool complete { get; internal set; }

    public override IActionStrategy Create(IGoapAgent agent) {
      return new BuildAtCampStrategy(agent, this);
    }

    public override void OnStart() {
      complete = false;
      _state = BuildState.SelectingRecipe;
      SelectRecipeAndSpot();
    }

    public override void OnUpdate(float deltaTime) {
      switch (_state) {
        case BuildState.SelectingRecipe:
          break;
        case BuildState.MovingToSpot:
          UpdateMoving();
          break;
        case BuildState.Building:
          _buildTimer?.Tick();
          break;
        case BuildState.Done:
          complete = true;
          break;
      }
    }

    private void SelectRecipeAndSpot() {
      // Get agent's camp
      _camp = _agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      if (_camp == null || _camp.setup == null) {
        Debug.LogWarning("[BuildAtCamp] No camp or setup found");
        _state = BuildState.Done;
        return;
      }

      // Get unlocked camp recipes sorted by priority
      var campRecipes = _agent.recipes.GetUnlockedCampRecipes(_recipeModule);

      // Find first recipe we can craft that has an empty spot
      foreach (var recipe in campRecipes) {
        if (!_recipeModule.CanCraft(recipe, _agent.inventory)) continue;

        // Find empty spot matching this recipe's tag
        var spot = FindSpotForRecipe(recipe);
        if (spot == null) continue;

        _selectedRecipe = recipe;
        _targetSpot = spot;
        break;
      }

      if (_selectedRecipe == null || _targetSpot == null) {
        Debug.Log("[BuildAtCamp] No buildable recipe or available spot");
        _state = BuildState.Done;
        return;
      }

      Debug.Log($"[BuildAtCamp] Selected: {_selectedRecipe.recipeId} at spot {_targetSpot.name}");
      _state = BuildState.MovingToSpot;
      _agent.navMeshAgent.SetDestination(_targetSpot.position);
    }

    private CampSpot FindSpotForRecipe(RecipeSO recipe) {
      var tags = _recipeModule.GetResultActorTags(recipe);
      var tag = tags[0];
      // If recipe has specific tag, find spot that prefers it
      if (!string.IsNullOrEmpty(tag)) {
        var preferredSpot = _camp.setup.GetSpotsNeedingTag(tag).FirstOrDefault();
        if (preferredSpot != null) return preferredSpot;
      }

      // Otherwise any empty spot
      return _camp.setup.GetAnyEmptySpot();
    }

    private void UpdateMoving() {
      if (_targetSpot == null || !_targetSpot.isEmpty) {
        // Spot taken - reselect
        SelectRecipeAndSpot();
        return;
      }

      var nav = _agent.navMeshAgent;
      if (nav.pathPending) return;

      if (nav.remainingDistance <= 2f) {
        StartBuilding();
      }
    }

    private void StartBuilding() {
      _state = BuildState.Building;
      _agent.navMeshAgent.ResetPath();

      // Consume resources
      ConsumeResources();

      // Start build timer
      _buildTimer?.Dispose();
      _buildTimer = new CountdownTimer(_selectedRecipe.recipe.craftTime);
      _buildTimer.OnTimerStop += OnBuildComplete;
      _buildTimer.Start();

      Debug.Log($"[BuildAtCamp] Building {_selectedRecipe.recipeId}...");
    }

    private void ConsumeResources() {
      foreach (var requiredResource in _selectedRecipe.recipe.requiredResources) {
        var remaining = (int)requiredResource.count;
        while (remaining > 0) {
          if (!_agent.inventory.TryGetSlotWithItemTags(new[] { requiredResource.tag }, out var slot)) break;

          var toRemove = Mathf.Min(remaining, slot.count);
          slot.RemoveCount(toRemove);
          remaining -= toRemove;
          if (slot.count == 0) {
            slot.ClearSlot();
          }
        }
      }
    }

    private void OnBuildComplete() {
      if (_targetSpot == null || !_targetSpot.isEmpty) {
        _state = BuildState.Done;
        return;
      }

      // Spawn actor
      if (_actorCreation.TrySpawnActor(
            _selectedRecipe.recipe.resultActorKey,
            _targetSpot.position,
            out var actor,
            _selectedRecipe.recipe.outputCount)) {
        _targetSpot.SetBuiltActor(actor);
        _agent.AddExperience(10); // XP for building
        Debug.Log($"[BuildAtCamp] Built {actor.actorKey} at {_targetSpot.name}");
      }
      else {
        Debug.LogError($"[BuildAtCamp] Failed to spawn {_selectedRecipe.recipe.resultActorKey}");
      }

      _state = BuildState.Done;
    }

    public override void OnStop() {
      _buildTimer?.Dispose();
      _agent.navMeshAgent.ResetPath();
      _targetSpot = null;
      _selectedRecipe = null;
      _camp = null;
    }

    public override void OnComplete() {
      Debug.Log($"[BuildAtCamp] Strategy complete");
    }

    private enum BuildState {
      SelectingRecipe,
      MovingToSpot,
      Building,
      Done
    }
  }
}