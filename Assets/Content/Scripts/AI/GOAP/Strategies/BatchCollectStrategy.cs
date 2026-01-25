using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.Navigation;
using Content.Scripts.Animation;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class BatchCollectResourcesStrategy : BatchCollectStrategy {
    public BatchCollectResourcesStrategy() : base() {
    }

    private BatchCollectResourcesStrategy(IGoapAgentCore agent, BatchCollectResourcesStrategy template)
      : base(agent, template) {
    }

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new BatchCollectResourcesStrategy(agent, this);
    }

    protected override bool IsCollectedEnough() {
       return _inventoryAgent.inventory.isFull || _collectedCount >= targetCount;
    }

    protected override bool IsTargetActorRequired(MemorySnapshot m) {
      if (m.target == null) return false;
      var desc = m.target.GetComponent<ActorDescription>();
      if (desc == null || !desc.collectable) return false;
      return desc.descriptionData.tags.Any(UnfinishedQuery.IsResourceNeeded);
    }

    protected override void OnItemPickedUp(ActorDescription pickedItem) {
      base.OnItemPickedUp(pickedItem);
      Debug.LogError($"BatchCollectResourcesStrategy picked up: {pickedItem.name} {_collectedCount}");
      
    }
  }
  
  /// <summary>
  ///   Collects multiple items in a loop: Move → Pickup → repeat until done.
  ///   Done when: inventory full OR collected targetCount OR no more items in range.
  /// </summary>
  [Serializable]
  public class BatchCollectStrategy : AgentStrategy {
    [MinValue(1)] public int targetCount = 4;
    public float searchRadius = 30f;
    public float pickupDuration = 1f;
    [ValueDropdown("GetTags")] public string[] itemTags = { Tag.ITEM };

    protected IGoapAgentCore _agent;
    protected IInventoryAgent _inventoryAgent;
    protected ITransientTargetAgent _transientAgent;
    protected UniversalAnimationController _animations;
    protected int _collectedCount;
    protected MemorySnapshot _currentTarget;
    protected SimTimer _pickupTimer;
    protected BatchState _state;

    public BatchCollectStrategy() {
    }
    
    protected BatchCollectStrategy(IGoapAgentCore agent, BatchCollectStrategy template) {
      _agent = agent;
      _inventoryAgent = agent as IInventoryAgent;
      _transientAgent = agent as ITransientTargetAgent;
      _animations = agent.body?.animationController;
      targetCount = template.targetCount;
      searchRadius = template.searchRadius;
      pickupDuration = template.pickupDuration;
      itemTags = template.itemTags;
    }

    public override bool canPerform => !complete;
    public override bool complete { get; internal set; }

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new BatchCollectStrategy(agent, this);
    }

    public override void OnStart() {
      _collectedCount = 0;
      _state = BatchState.SearchingTarget;
      complete = false;
      
      if (_inventoryAgent == null || _transientAgent == null) {
        Debug.LogWarning("[BatchCollect] Agent missing IInventoryAgent or ITransientTargetAgent");
        complete = true;
        return;
      }
      
      InitTimer();
      FindNextTarget();
    }

    private void InitTimer() {
      _pickupTimer?.Dispose();
      _pickupTimer = new SimTimer(pickupDuration);
      _pickupTimer.OnTimerComplete += OnPickupComplete;
    }

    public override void OnUpdate(float deltaTime) {
      switch (_state) {
        case BatchState.SearchingTarget:
          break;

        case BatchState.MovingToTarget:
          UpdateMoving();
          break;

        case BatchState.PickingUp:
          _pickupTimer?.Tick(deltaTime);
          break;

        case BatchState.Done:
          complete = true;
          break;
      }
    }

    private void FindNextTarget() {
      if (_inventoryAgent == null || IsCollectedEnough()) {
        _state = BatchState.Done;
        return;
      }

      var target = GetNextTarget();

      if (target == null) {
        _state = BatchState.Done;
        return;
      }

      _currentTarget = target;
      _state = BatchState.MovingToTarget;
      _agent.navMeshAgent.SetDestination(_currentTarget.location);
    }

    protected virtual bool IsCollectedEnough() {
      return _inventoryAgent.inventory.isFull || _collectedCount >= targetCount;
    }

    protected virtual MemorySnapshot GetNextTarget() {
      var tags = itemTags.Length > 0 ? itemTags : new[] { Tag.ITEM };
      var candidates = _agent.memory.GetInRadius(_agent.position, searchRadius, tags);
      
      var target = PathCostEvaluator.GetNearestReachable(
        _agent.navMeshAgent,
        candidates,
        IsTargetActorRequired
      );
      return target;
    }

    protected virtual bool IsTargetActorRequired(MemorySnapshot m) {
      if (m.target == null) return false;
      var desc = m.target.GetComponent<ActorDescription>();
      if (desc == null || !desc.collectable) return false;
      return StorageQuery.AnyStorageNeeds(desc);
    }

    private void UpdateMoving() {
      if (_currentTarget?.target == null) {
        FindNextTarget();
        return;
      }

      var nav = _agent.navMeshAgent;
      if (nav.pathPending) return;

      if (nav.remainingDistance <= 2f) StartPickup();
    }

    private void StartPickup() {
      if (_currentTarget?.target == null) {
        FindNextTarget();
        return;
      }

      _state = BatchState.PickingUp;
      _agent.navMeshAgent.ResetPath();
      if (_transientAgent != null) {
        _transientAgent.transientTarget = _currentTarget.target;
      }
      _animations?.PickUp();
      _pickupTimer.Start();
    }

    private void OnPickupComplete() {
      if (_transientAgent?.transientTarget != null && _inventoryAgent != null) {
        var target = _transientAgent.transientTarget.GetComponent<ActorDescription>();
        if (target != null && _inventoryAgent.inventory.TryPutItemInInventory(target)) {
          OnItemPickedUp(target);
          Debug.Log($"[BatchCollect] Picked up {target.name}, total: {_collectedCount}/{targetCount}");
          _agent.memory.Forget(_currentTarget);
        }
      }

      if (_transientAgent != null) {
        _transientAgent.transientTarget = null;
      }
      _currentTarget = null;

      FindNextTarget();
    }

    protected virtual void OnItemPickedUp(ActorDescription pickedItem) {
      _collectedCount++;
    }

    public override void OnStop() {
      _pickupTimer?.Dispose();
      _pickupTimer = null;
      _agent?.navMeshAgent?.ResetPath();
      if (_transientAgent != null) {
        _transientAgent.transientTarget = null;
      }
      _currentTarget = null;
    }

    public override void OnComplete() {
      Debug.Log($"[BatchCollect] Complete. Collected {_collectedCount} items. [{_state}]");
    }

    public enum BatchState {
      SearchingTarget,
      MovingToTarget,
      PickingUp,
      Done
    }
  }
}
