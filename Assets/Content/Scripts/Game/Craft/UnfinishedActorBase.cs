using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Game.Craft {
  [RequireComponent(typeof(ActorDescription))]
  [RequireComponent(typeof(ActorInventory))]
  public abstract class UnfinishedActorBase : MonoBehaviour, IUnfinishedActor {
    [ShowInInspector, ReadOnly, Range(0, 1f)]
    protected float _workProgress;

    [Inject] protected ActorCreationModule _actorCreation;

    protected ActorDescription _description;
    protected ActorInventory _inventory;

    public ActorDescription actor => _description;
    public ActorInventory inventory => _inventory;

    public float workProgress => _workProgress;
    public abstract float workRequired { get; }
    public float workRatio => workRequired > 0f ? Mathf.Clamp01(_workProgress / workRequired) : 1f;
    public bool workComplete => _workProgress >= workRequired;
    public virtual float progress => workRatio;
    [ShowInInspector] public bool hasAllResources { get; protected set; }
    public bool isReadyToComplete => hasAllResources && workComplete;

    protected abstract IReadOnlyList<RecipeRequiredResource> requiredResources { get; }

    protected virtual void Awake() {
      _description = GetComponent<ActorDescription>();
      _inventory = GetComponent<ActorInventory>();
    }

    protected virtual void OnEnable() {
      ActorRegistry<UnfinishedActorBase>.Register(this);
    }

    protected virtual void OnDisable() {
      ActorRegistry<UnfinishedActorBase>.Unregister(this);
    }

    public virtual bool AddWork(float amount) {
      if (amount <= 0f) return workComplete;
      _workProgress = Mathf.Min(_workProgress + amount, workRequired);
      return workComplete;
    }

    public virtual int GetRemainingResourceCount(string tag) {
      if (requiredResources == null) return 0;
      var required = requiredResources
        .Where(r => r.tag == tag)
        .Sum(r => r.count);
      var have = _inventory.GetItemCount(tag);
      return Mathf.Max(0, required - have);
    }

    public virtual (string tag, int remaining)[] GetRemainingResources() {
      if (requiredResources == null) return System.Array.Empty<(string, int)>();
      return requiredResources
        .Select(r => (r.tag, GetRemainingResourceCount(r.tag)))
        .Where(x => x.Item2 > 0)
        .ToArray();
    }

    public virtual bool CheckAllResourcesDelivered() {
      if (requiredResources == null) return true;
      foreach (var req in requiredResources) {
        if (GetRemainingResourceCount(req.tag) > 0) return false;
      }
      hasAllResources = true;
      return true;
    }

    public abstract ActorDescription TryComplete();
  }
}

