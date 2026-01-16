using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.Animation;
using Content.Scripts.Game;
using Content.Scripts.Game.Storage;
using ImprovedTimers;
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

    private IGoapAgent _agent;
    private AnimationController _animations;
    private int _collectedCount;
    private MemorySnapshot _currentTarget;
    private CountdownTimer _pickupTimer;
    private BatchState _state;

    public BatchCollectStrategy() {
    }

    private BatchCollectStrategy(IGoapAgent agent, BatchCollectStrategy template) {
      _agent = agent;
      _animations = agent.animationController;
      targetCount = template.targetCount;
      searchRadius = template.searchRadius;
      pickupDuration = template.pickupDuration;
      itemTags = template.itemTags;
    }

    public override bool canPerform => !complete;
    public override bool complete { get; internal set; }

    public override IActionStrategy Create(IGoapAgent agent) {
      return new BatchCollectStrategy(agent, this);
    }

    public override void OnStart() {
      _collectedCount = 0;
      _state = BatchState.SearchingTarget;
      complete = false;
      InitTimer();
      FindNextTarget();
    }

    private void InitTimer() {
      _pickupTimer?.Dispose();
      _pickupTimer = new CountdownTimer(pickupDuration);
      _pickupTimer.OnTimerStop += OnPickupComplete;
    }

    public override void OnUpdate(float deltaTime) {
      switch (_state) {
        case BatchState.SearchingTarget:
          // Handled in FindNextTarget
          break;

        case BatchState.MovingToTarget:
          UpdateMoving();
          break;

        case BatchState.PickingUp:
          _pickupTimer.Tick();
          break;

        case BatchState.Done:
          complete = true;
          break;
      }
    }

    private void FindNextTarget() {
      // Check stop conditions
      if (_agent.inventory.isFull || _collectedCount >= targetCount) {
        _state = BatchState.Done;
        return;
      }

      // Find nearest collectible item in memory
      var tags = itemTags.Length > 0 ? itemTags : new[] { Tag.ITEM };
      _currentTarget = _agent.memory.GetNearest(
        _agent.position,
        searchRadius,
        tags,
        IsValidTarget
      );

      if (_currentTarget == null) {
        // No more items - done if we collected something
        _state = BatchState.Done;
        return;
      }

      // Move to target
      _state = BatchState.MovingToTarget;
      _agent.navMeshAgent.SetDestination(_currentTarget.location);
    }

    private bool IsValidTarget(MemorySnapshot m) {
      if (m.target == null) return false;
      var desc = m.target.GetComponent<ActorDescription>();
      if (desc == null || !desc.collectable) return false;
      // Check if any storage needs this item
      return StorageQuery.AnyStorageNeeds(desc);
    }

    private void UpdateMoving() {
      if (_currentTarget?.target == null) {
        // Target disappeared - find next
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
      _agent.transientTarget = _currentTarget.target;
      _animations?.PickUp();
      _pickupTimer.Start();
    }

    private void OnPickupComplete() {
      if (_agent.transientTarget != null) {
        var target = _agent.transientTarget.GetComponent<ActorDescription>();
        if (target != null && _agent.inventory.TryPutItemInInventory(target)) {
          _collectedCount++;
          Debug.Log($"[BatchCollect] Picked up {target.name}, total: {_collectedCount}/{targetCount}");

          // Remove from memory since we picked it up
          _agent.memory.Forget(_currentTarget);
        }
      }

      _agent.transientTarget = null;
      _currentTarget = null;

      // Find next target
      FindNextTarget();
    }

    public override void OnStop() {
      _pickupTimer?.Dispose();
      _agent.navMeshAgent.ResetPath();
      _agent.transientTarget = null;
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