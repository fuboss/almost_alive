using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.Navigation;
using Content.Scripts.Animation;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies {
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

    private IGoapAgentCore _agent;
    private IInventoryAgent _inventoryAgent;
    private ITransientTargetAgent _transientAgent;
    private AnimationController _animations;
    private int _collectedCount;
    private MemorySnapshot _currentTarget;
    private SimTimer _pickupTimer;
    private BatchState _state;

    public BatchCollectStrategy() {
    }

    private BatchCollectStrategy(IGoapAgentCore agent, BatchCollectStrategy template) {
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
      if (_inventoryAgent == null) {
        _state = BatchState.Done;
        return;
      }
      
      if (_inventoryAgent.inventory.isFull || _collectedCount >= targetCount) {
        _state = BatchState.Done;
        return;
      }

      var tags = itemTags.Length > 0 ? itemTags : new[] { Tag.ITEM };
      var candidates = _agent.memory.GetInRadius(_agent.position, searchRadius, tags);
      
      _currentTarget = PathCostEvaluator.GetNearestReachable(
        _agent.navMeshAgent,
        candidates,
        IsValidTarget
      );

      if (_currentTarget == null) {
        _state = BatchState.Done;
        return;
      }

      _state = BatchState.MovingToTarget;
      _agent.navMeshAgent.SetDestination(_currentTarget.location);
    }

    private bool IsValidTarget(MemorySnapshot m) {
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
          _collectedCount++;
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

    private enum BatchState {
      SearchingTarget,
      MovingToTarget,
      PickingUp,
      Done
    }
  }
}
