using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Content.Scripts.Game.Storage;
using ImprovedTimers;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace Content.Scripts.AI.GOAP.Strategies.Craft {
  /// <summary>
  /// Delivers resources to unfinished actor.
  /// Sources: agent inventory first, then camp storages.
  /// Delivers one resource type at a time.
  /// </summary>
  [Serializable]
  public class DeliverToUnfinishedStrategy : AgentStrategy {
    private IGoapAgent _agent;
    private CampLocation _camp;
    private UnfinishedActor _target;
    private DeliverState _state;

    private string _targetTag;
    private int _targetCount;
    private StorageActor _sourceStorage;
    private bool _useAgentInventory;
    private CountdownTimer _timer;
    [Inject] private ActorCreationModule _creationModule;

    public DeliverToUnfinishedStrategy() {
    }

    private DeliverToUnfinishedStrategy(IGoapAgent agent, DeliverToUnfinishedStrategy template) {
      _agent = agent;
    }

    public override bool canPerform => true;
    public override bool complete { get; internal set; }

    public override IActionStrategy Create(IGoapAgent agent) {
      return new DeliverToUnfinishedStrategy(agent, this);
    }

    public override void OnStart() {
      complete = false;
      _state = DeliverState.FindTarget;
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick();
      switch (_state) {
        case DeliverState.FindTarget:
          FindTarget();
          break;
        case DeliverState.FindResource:
          FindResourceSource();
          break;
        case DeliverState.MoveToSource:
          UpdateMoveToSource();
          break;
        case DeliverState.PickupFromStorage:
          PickupFromStorage();
          break;
        case DeliverState.MoveToTarget:
          UpdateMoveToTarget();
          break;
        case DeliverState.Deliver:
          DeliverToTarget();
          break;
        case DeliverState.Done:

          if (_timer == null) {
            _timer = new CountdownTimer(1f);
            _timer.OnTimerStop += () => {
              complete = true;
              _timer.Dispose();
              _timer = null;
            };
            _timer.Start();
          }

          break;
      }
    }

    private void FindTarget() {
      _camp = _agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      _target = UnfinishedQuery.GetNeedingResources(_camp);

      if (_target == null) {
        Debug.Log("[DeliverUnfinished] No target needing resources");
        _state = DeliverState.Done;
        return;
      }

      _state = DeliverState.FindResource;
    }

    private void FindResourceSource() {
      var needs = _target.GetRemainingResources();
      if (needs.Length == 0) {
        Debug.Log("[DeliverUnfinished] Target has all resources");
        _state = DeliverState.Done;
        return;
      }

      foreach (var (tag, needed) in needs) {
        // Check agent inventory first
        var inInventory = _agent.inventory.GetItemCount(tag);
        if (inInventory > 0) {
          _targetTag = tag;
          _targetCount = Mathf.Min(inInventory, needed);
          _useAgentInventory = true;
          _sourceStorage = null;
          Debug.Log($"[DeliverUnfinished] Using {_targetCount}x {tag} from inventory");
          _state = DeliverState.MoveToTarget;
          _agent.navMeshAgent.SetDestination(_target.transform.position);
          return;
        }

        // Check camp storages
        var storage = FindStorageWithResource(tag);
        if (storage != null) {
          _targetTag = tag;
          _targetCount = Mathf.Min(storage.GetCountWithTag(tag), needed);
          _useAgentInventory = false;
          _sourceStorage = storage;
          Debug.Log($"[DeliverUnfinished] Getting {_targetCount}x {tag} from storage");
          _state = DeliverState.MoveToSource;
          _agent.navMeshAgent.SetDestination(storage.transform.position);
          return;
        }
      }

      Debug.Log("[DeliverUnfinished] No resources available");
      _state = DeliverState.Done;
    }

    private StorageActor FindStorageWithResource(string tag) {
      return ActorRegistry<StorageActor>.all
        .Where(s => IsAtCamp(s.transform.position))
        .FirstOrDefault(s => s.GetCountWithTag(tag) > 0);
    }

    private bool IsAtCamp(Vector3 pos) {
      if (_camp == null) return false;
      return Vector3.Distance(pos, _camp.transform.position) < 30f;
    }

    private void UpdateMoveToSource() {
      var nav = _agent.navMeshAgent;
      if (nav.pathPending) return;
      if (nav.remainingDistance <= 2f) _state = DeliverState.PickupFromStorage;
    }

    private void PickupFromStorage() {
      if (_sourceStorage == null) {
        _state = DeliverState.FindResource;
        return;
      }

      var taken = 0;
      var inv = _sourceStorage.inventory;

      while (taken < _targetCount && !_agent.inventory.isFull) {
        if (!inv.TryGetSlotWithItemTags(new[] { _targetTag }, out var slot)) break;

        if (slot.ReleaseSingle(out var item, _agent.position)) {
          _agent.inventory.TryPutItemInInventory(item);
          taken++;
        }
        else {
          slot.RemoveCount(1);
          taken++;
        }
      }

      Debug.Log($"[DeliverUnfinished] Picked up {taken}x {_targetTag}");
      _targetCount = taken;

      if (taken > 0) {
        _state = DeliverState.MoveToTarget;
        _agent.navMeshAgent.SetDestination(_target.transform.position);
      }
      else {
        _state = DeliverState.FindResource;
      }
    }

    private void UpdateMoveToTarget() {
      if (_target == null) {
        _state = DeliverState.Done;
        return;
      }

      var nav = _agent.navMeshAgent;
      if (nav.pathPending) return;
      if (nav.remainingDistance <= 2f) _state = DeliverState.Deliver;
    }

    private void DeliverToTarget() {
      if (_target == null) {
        _state = DeliverState.Done;
        return;
      }

      var delivered = 0;
      while (delivered < _targetCount) {
        if (!_agent.inventory.TryGetSlotWithItemTags(new[] { _targetTag }, out var slot)) break;

        if (slot.ReleaseSingle(out var item, _target.transform.position)) {
          if (_target.inventory.TryPutItemInInventory(item)) {
            delivered++;
          }
          else {
            Debug.LogError("failed to deliver item to target, returning to agent inventory");
            _agent.inventory.TryPutItemInInventory(item);
            break;
          }
        }
        else {
          if (_creationModule.TrySpawnActor(slot.item.actorKey, _target.transform.position, out var newItem)) {
            if (!_target.inventory.TryPutItemInInventory(newItem)) {
              Debug.LogError("failed to deliver spawned item to target, destroying it");
              Object.Destroy(newItem.gameObject);
            }

            slot.RemoveCount(1);
            delivered++;
            break;
          }
          else {
            Debug.LogError("failed to spawn item for delivery");
            break;
          }
        }
      }

      Debug.Log($"[DeliverUnfinished] Delivered {delivered}x {_targetTag}");
      _state = DeliverState.Done;
    }

    public override void OnStop() {
      _agent?.navMeshAgent?.ResetPath();
      _target = null;
      _sourceStorage = null;
      _camp = null;
    }

    private enum DeliverState {
      FindTarget,
      FindResource,
      MoveToSource,
      PickupFromStorage,
      MoveToTarget,
      Deliver,
      Done
    }
  }
}