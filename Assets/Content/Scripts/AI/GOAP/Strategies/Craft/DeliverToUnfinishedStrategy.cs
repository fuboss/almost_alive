using System;
using System.Collections.Generic;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using ImprovedTimers;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace Content.Scripts.AI.GOAP.Strategies.Craft {
  [Serializable]
  public class DeliverToUnfinishedStrategy : AgentStrategy {
    public float duration = 2f;
    private IGoapAgent _agent;
    private CampLocation _camp;
    private UnfinishedActor _target;
    private bool _abort;
    private CountdownTimer _timer;

    [Inject] protected ActorCreationModule _creationModule;
    private bool _complete;

    public DeliverToUnfinishedStrategy() {
    }

    private DeliverToUnfinishedStrategy(IGoapAgent agent, DeliverToUnfinishedStrategy template) {
      _agent = agent;
      duration = template.duration;
    }

    public override bool canPerform => true;

    public override bool complete {
      get => _complete;
      internal set { _complete = value; }
    }

    public override IActionStrategy Create(IGoapAgent agent) {
      return new DeliverToUnfinishedStrategy(agent, this);
    }

    public override void OnStart() {
      complete = false;
      FindTarget();
      if (_target == null) {
        complete = true;
        return;
      }

      IniTimer();
    }


    private void IniTimer() {
      _timer?.Dispose();
      _timer = new CountdownTimer(duration); //animations.GetAnimationLength(animations.)

      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
      _timer.Start();
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick();
      if (_abort) {
        complete = true;
        _timer?.Stop();
        Debug.LogError($"[DeliverUnfinished] Aborting delivery strategy");
      }
    }

    public override void OnComplete() {
      base.OnComplete();
      DeliverToTarget();
    }

    protected void FindTarget() {
      var unfinishedActor = _agent.transientTarget != null
        ? _agent.transientTarget.GetComponent<UnfinishedActor>()
        : null;
      if (unfinishedActor != null) {
        _target = unfinishedActor;
        Debug.Log($"[DeliverUnfinished] Using transient target {_target.recipe.recipeId}");
        _agent.navMeshAgent.SetDestination(_target.transform.position);
        return;
      }

      _camp = _agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      _target = UnfinishedQuery.GetNeedingResources(_camp);

      if (_target == null) {
        Debug.Log("[DeliverUnfinished] No target needing resources");
        _abort = true;
      }
    }

    private void DeliverToTarget() {
      if (_target == null) {
        _abort = true;
        return;
      }

      var needs = _target.GetRemainingResources();
      var targetInventory = _target.inventory;
      int delivered = 0;

      foreach (var (tag, remaining) in needs) {
        if (!_agent.inventory.TryGetSlotWithItemTags(new[] { tag }, out var slot)) continue;
        var count = slot.count;
        if (count >= remaining) {
          if (_creationModule.TrySpawnActor(slot.item.actorKey, _target.transform.position, out var newItem)) {
            newItem.GetStackData().current = remaining;
            if (targetInventory.TryPutItemInInventory(newItem, remaining)) {
              Debug.Log($"Delivered full stack of {tag} x {remaining}");
            }
            else {
              Debug.LogError($"Failed to deliver full stack of {tag} to target, destroying spawned item");
              Object.Destroy(newItem.gameObject);
            }
          }
          else {
            Debug.LogError($"Failed to spawn newItem '{slot.item.actorKey}' for delivery");
          }

          slot.RemoveCount(remaining);
          delivered += remaining;
          continue;
        }

        if (slot.Release(out var depositActor)) {
          if (targetInventory.TryPutItemInInventory(depositActor)) {
            Debug.Log($"Delivered full stack of {tag}");
            delivered += depositActor.GetStackData().current;
          }
          else {
            Debug.LogError($"Failed to deliver full stack of {tag} to target, destroying spawned item");
            Object.Destroy(depositActor.gameObject);
          }
        }
        else {
          Debug.LogError($"Failed to release item from agent inventory for delivery. from slot {slot.index}");
        }
      }

      Debug.Log($"Delivered total of {delivered} items to {_target.name}");
    }

    public override void OnStop() {
      _agent.StopAndCleanPath();
      _target = null;
      _camp = null;
    }
  }
}