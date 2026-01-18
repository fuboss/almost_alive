using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using UnityEngine;
using VContainer;

namespace Content.Scripts.AI.GOAP.Strategies.Craft {
  /// <summary>
  /// Places a new unfinished actor at an empty camp spot.
  /// Selects highest-priority recipe that has an available spot.
  /// Does NOT require resources - just creates the blueprint.
  /// </summary>
  [Serializable]
  public class PlaceUnfinishedStrategy : AgentStrategy {
    [Inject] private RecipeModule _recipeModule;
    [Inject] private ActorCreationModule _actorCreation;
    [Inject] private IObjectResolver _resolver;

    private IGoapAgent _agent;
    private CampLocation _camp;
    private CampSpot _targetSpot;
    private RecipeSO _selectedRecipe;
    private PlaceState _state;

    private const string UNFINISHED_KEY = "unfinished";

    public PlaceUnfinishedStrategy() {
    }

    private PlaceUnfinishedStrategy(IGoapAgent agent, PlaceUnfinishedStrategy template) {
      _agent = agent;
      _recipeModule = template._recipeModule;
      _actorCreation = template._actorCreation;
      _resolver = template._resolver;
    }

    public override bool canPerform => _recipeModule != null && _actorCreation != null;
    public override bool complete { get; internal set; }

    public override IActionStrategy Create(IGoapAgent agent) {
      return new PlaceUnfinishedStrategy(agent, this);
    }

    public override void OnStart() {
      complete = false;
      _state = PlaceState.Selecting;
      SelectRecipeAndSpot();
    }

    public override void OnUpdate(float deltaTime) {
      switch (_state) {
        case PlaceState.Selecting:
          break;
        case PlaceState.Moving:
          UpdateMoving();
          break;
        case PlaceState.Placing:
          PlaceUnfinished();
          break;
        case PlaceState.Done:
          complete = true;
          break;
      }
    }

    private void SelectRecipeAndSpot() {
      _camp = _agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      if (_camp?.setup == null) {
        Debug.LogWarning("[PlaceUnfinished] No camp or setup");
        _state = PlaceState.Done;
        return;
      }

      // Skip if there's already an active unfinished
      if (UnfinishedQuery.HasActiveUnfinished(_camp)) {
        Debug.Log("[PlaceUnfinished] Already has active unfinished");
        _state = PlaceState.Done;
        return;
      }

      // Get unlocked camp recipes by priority (don't check resources!)
      var campRecipes = _agent.recipes.GetUnlockedCampRecipes(_recipeModule);

      foreach (var recipe in campRecipes) {
        var spot = FindSpotForRecipe(recipe);
        if (spot == null) continue;

        _selectedRecipe = recipe;
        _targetSpot = spot;
        break;
      }

      if (_selectedRecipe == null || _targetSpot == null) {
        Debug.Log("[PlaceUnfinished] No available recipe or spot");
        _state = PlaceState.Done;
        return;
      }

      Debug.Log($"[PlaceUnfinished] Selected {_selectedRecipe.recipeId} at {_targetSpot.name}");
      _state = PlaceState.Moving;
      _agent.navMeshAgent.SetDestination(_targetSpot.position);
    }

    private CampSpot FindSpotForRecipe(RecipeSO recipe) {
      var tags = _recipeModule.GetResultActorTags(recipe);
      var tag = tags?.Length > 0 ? tags[0] : null;

      if (!string.IsNullOrEmpty(tag)) {
        var preferred = _camp.setup.GetSpotsNeedingTag(tag).FirstOrDefault();
        if (preferred != null) return preferred;
      }

      return _camp.setup.GetAnyEmptySpot();
    }

    private void UpdateMoving() {
      if (_targetSpot == null || !_targetSpot.isEmpty) {
        SelectRecipeAndSpot();
        return;
      }

      var nav = _agent.navMeshAgent;
      if (nav.pathPending) return;

      if (nav.remainingDistance <= 2f) {
        _state = PlaceState.Placing;
      }
    }

    private void PlaceUnfinished() {
      _agent.navMeshAgent.ResetPath();

      // Spawn unfinished prefab
      if (!_actorCreation.TrySpawnActor(UNFINISHED_KEY, _targetSpot.position, out var actor)) {
        Debug.LogError("[PlaceUnfinished] Failed to spawn unfinished prefab");
        _state = PlaceState.Done;
        return;
      }

      var unfinished = actor.GetComponent<UnfinishedActor>();
      if (unfinished == null) {
        Debug.LogError("[PlaceUnfinished] Prefab missing UnfinishedActor component!");
        UnityEngine.Object.Destroy(actor.gameObject);
        _state = PlaceState.Done;
        return;
      }

      unfinished.Initialize(_selectedRecipe, _targetSpot);

      // Parent to spot
      actor.transform.SetParent(_targetSpot.transform);
      actor.transform.localPosition = Vector3.zero;
      _agent.transientTarget = unfinished.description;

      Debug.Log($"[PlaceUnfinished] Placed for {_selectedRecipe.recipeId}");
      _state = PlaceState.Done;
    }

    public override void OnStop() {
      _agent?.navMeshAgent?.ResetPath();
      _agent.transientTarget = null;
      _targetSpot = null;
      _selectedRecipe = null;
      _camp = null;
    }

    private enum PlaceState {
      Selecting,
      Moving,
      Placing,
      Done
    }
  }
}