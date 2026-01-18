using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Game.Craft {
  /// <summary>
  /// Unfinished actor - intermediate state during crafting/building.
  /// Stores recipe reference, required resources inventory, and work progress.
  /// </summary>
  [RequireComponent(typeof(ActorDescription))]
  [RequireComponent(typeof(ActorInventory))]
  public class UnfinishedActor : MonoBehaviour {
    [ShowInInspector, ReadOnly] private RecipeSO _recipe;
    [ShowInInspector, ReadOnly, Range(0,1f)] private float _workProgress;
    [ShowInInspector, ReadOnly] private CampSpot _assignedSpot;
    
    [Inject] private ActorCreationModule _actorCreation;

    private ActorDescription _description;
    private ActorInventory _inventory;

    public RecipeSO recipe => _recipe;
    public ActorDescription description => _description;
    public ActorInventory inventory => _inventory;
    [ShowInInspector]public CampSpot assignedSpot => _assignedSpot;
    
    public float workProgress => _workProgress;
    public float workRequired => _recipe?.recipe.workRequired ?? 0f;
    public float workRatio => workRequired > 0f ? Mathf.Clamp01(_workProgress / workRequired) : 1f;
    public bool workComplete => _workProgress >= workRequired;
    
    [ShowInInspector]public bool hasAllResources => CheckAllResourcesDelivered();
    public bool isReadyToComplete => hasAllResources && workComplete;

    private void Awake() {
      _description = GetComponent<ActorDescription>();
      _inventory = GetComponent<ActorInventory>();
    }

    private void OnEnable() {
      ActorRegistry<UnfinishedActor>.Register(this);
    }

    private void OnDisable() {
      ActorRegistry<UnfinishedActor>.Unregister(this);
    }

    /// <summary>Initialize with recipe.</summary>
    public void Initialize(RecipeSO recipe, CampSpot spot = null) {
      _recipe = recipe;
      _assignedSpot = spot;
      _workProgress = 0f;
      Debug.Log($"[Unfinished] Initialized for {recipe.recipeId}" + (spot != null ? $" at {spot.name}" : ""));
    }

    /// <summary>Add work progress. Returns true if work is complete.</summary>
    public bool AddWork(float amount) {
      if (amount <= 0f) return workComplete;
      _workProgress = Mathf.Min(_workProgress + amount, workRequired);
      return workComplete;
    }

    /// <summary>Get remaining resource count for specific tag.</summary>
    public int GetRemainingResourceCount(string tag) {
      var required = _recipe.recipe.requiredResources
        .Where(r => r.tag == tag)
        .Sum(r => r.count);
      var have = _inventory.GetItemCount(tag);
      return Mathf.Max(0, required - have);
    }

    /// <summary>Get all remaining resource requirements.</summary>
    public (string tag, int remaining)[] GetRemainingResources() {
      return _recipe.recipe.requiredResources
        .Select(r => (r.tag, GetRemainingResourceCount(r.tag)))
        .Where(x => x.Item2 > 0)
        .ToArray();
    }

    /// <summary>Check if all required resources have been delivered.</summary>
    public bool CheckAllResourcesDelivered() {
      if (_recipe == null) return false;
      foreach (var req in _recipe.recipe.requiredResources) {
        if (GetRemainingResourceCount(req.tag) > 0) return false;
      }

      return true;
    }

    /// <summary>
    /// Try to complete. Spawns result actor and destroys this.
    /// Returns spawned actor or null on failure.
    /// </summary>
    public ActorDescription TryComplete() {
      if (!isReadyToComplete) {
        Debug.LogWarning($"[Unfinished] Cannot complete - resources: {hasAllResources}, work: {workComplete}");
        return null;
      }

      if (_actorCreation == null) {
        Debug.LogError("[Unfinished] ActorCreationModule not injected!");
        return null;
      }

      var pos = _assignedSpot != null ? _assignedSpot.position : transform.position;
      
      if (!_actorCreation.TrySpawnActor(_recipe.recipe.resultActorKey, pos, out var result, _recipe.recipe.outputCount)) {
        Debug.LogError($"[Unfinished] Failed to spawn {_recipe.recipe.resultActorKey}");
        return null;
      }

      if (_assignedSpot != null) {
        _assignedSpot.SetBuiltActor(result);
      }

      Debug.Log($"[Unfinished] Completed! Spawned {result.actorKey}");
      Destroy(gameObject);
      
      return result;
    }
  }
}
