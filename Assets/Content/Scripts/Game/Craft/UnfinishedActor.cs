using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Game.Craft {
  
  public interface IProgressProvider {
    float progress { get; }
    ActorDescription actor { get; }
  }
  /// <summary>
  /// Unfinished actor - intermediate state during crafting/building.
  /// Stores recipe reference, required resources inventory, and work progress.
  /// </summary>
  [RequireComponent(typeof(ActorDescription))]
  [RequireComponent(typeof(ActorInventory))]
  public class UnfinishedActor : MonoBehaviour, IProgressProvider {
    [ShowInInspector, ReadOnly] protected RecipeSO _recipe;
    [ShowInInspector, ReadOnly, Range(0,1f)] protected float _workProgress;
    
    [Inject] protected ActorCreationModule _actorCreation;

    protected ActorDescription _description;
    protected ActorInventory _inventory;

    public RecipeSO recipe => _recipe;
    public ActorDescription actor => _description;
    public ActorInventory inventory => _inventory;
    
    public float workProgress => _workProgress;
    public float workRequired => _recipe?.recipe.workRequired ?? 0f;
    public float workRatio => workRequired > 0f ? Mathf.Clamp01(_workProgress / workRequired) : 1f;
    public bool workComplete => _workProgress >= workRequired;
    float IProgressProvider.progress => workRatio;
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
    public void Initialize(RecipeSO recipe) {
      _recipe = recipe;
      _workProgress = 0f;
    }

    /// <summary>Add work progress. Returns true if work is complete.</summary>
    public virtual bool AddWork(float amount) {
      if (amount <= 0f) return workComplete;
      _workProgress = Mathf.Min(_workProgress + amount, workRequired);
      return workComplete;
    }

    /// <summary>Get remaining resource count for specific tag.</summary>
    public virtual int GetRemainingResourceCount(string tag) {
      var required = requiredResources
        .Where(r => r.tag == tag)
        .Sum(r => r.count);
      var have = _inventory.GetItemCount(tag);
      return Mathf.Max(0, required - have);
    }

    /// <summary>Get all remaining resource requirements.</summary>
    public virtual (string tag, int remaining)[] GetRemainingResources() {
      return requiredResources
        .Select(r => (r.tag, GetRemainingResourceCount(r.tag)))
        .Where(x => x.Item2 > 0)
        .ToArray();
    }

    /// <summary>Check if all required resources have been delivered.</summary>
    public virtual bool CheckAllResourcesDelivered() {
      if (_recipe == null) return false;
      foreach (var req in requiredResources) {
        if (GetRemainingResourceCount(req.tag) > 0) return false;
      }

      return true;
    }

    protected virtual IReadOnlyList<RecipeRequiredResource> requiredResources
      => _recipe.recipe.requiredResources;

    /// <summary>
    /// Try to complete. Spawns result actor and destroys this.
    /// Returns spawned actor or null on failure.
    /// </summary>
    public virtual ActorDescription TryComplete() {
      if (!isReadyToComplete) {
        Debug.LogWarning($"[Unfinished] Cannot complete - resources: {hasAllResources}, work: {workComplete}");
        return null;
      }

      if (_actorCreation == null) {
        Debug.LogError("[Unfinished] ActorCreationModule not injected!");
        return null;
      }

      var pos = transform.position;
      
      if (!_actorCreation.TrySpawnActorOnGround(_recipe.recipe.resultActorKey, pos, out var result, _recipe.recipe.outputCount)) {
        Debug.LogError($"[Unfinished] Failed to spawn {_recipe.recipe.resultActorKey}");
        return null;
      }

      Debug.Log($"[Unfinished] Completed! Spawned {result.actorKey}");
      Destroy(gameObject);
      
      return result;
    }
  }
}
