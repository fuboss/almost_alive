using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace Content.Scripts.AI.GOAP.Strategies.Craft {
  [Serializable]
  public class DeliverToUnfinishedStrategy : AgentStrategy {
    public float duration = 2f;
    
    private IGoapAgentCore _agent;
    private ITransientTargetAgent _transientAgent;
    private IInventoryAgent _inventoryAgent;
    private UnfinishedActor _target;
    private bool _abort;
    private SimTimer _timer;

    [Inject] protected ActorCreationModule _creationModule;
    private bool _complete;

    public DeliverToUnfinishedStrategy() {
    }

    private DeliverToUnfinishedStrategy(IGoapAgentCore agent, DeliverToUnfinishedStrategy template) {
      _agent = agent;
      _transientAgent = agent as ITransientTargetAgent;
      _inventoryAgent = agent as IInventoryAgent;
      duration = template.duration;
      _creationModule = template._creationModule;
    }

    public override bool canPerform => true;

    public override bool complete {
      get => _complete;
      internal set { _complete = value; }
    }

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new DeliverToUnfinishedStrategy(agent, this);
    }

    public override void OnStart() {
      complete = false;
      _abort = false;
      
      if (_inventoryAgent == null) {
        Debug.LogWarning("[DeliverUnfinished] Agent missing required interfaces");
        complete = true;
        return;
      }
      
      FindTarget();
      if (_target == null) {
        complete = true;
        return;
      }

      InitTimer();
    }

    private void InitTimer() {
      _timer?.Dispose();
      _timer = new SimTimer(duration);
      _timer.OnTimerComplete += () => complete = true;
      _timer.Start();
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick(deltaTime);
      if (_abort) {
        complete = true;
        _timer?.Stop();
        Debug.LogWarning("[DeliverUnfinished] Aborting delivery strategy");
      }
    }

    public override void OnComplete() {
      base.OnComplete();
      DeliverToTarget();
    }

    protected void FindTarget() {
      var unfinishedActor = _transientAgent?.transientTarget?.GetComponent<UnfinishedActor>();
      if (unfinishedActor != null) {
        _target = unfinishedActor;
        Debug.Log($"[DeliverUnfinished] Using transient target {_target.recipe.recipeId}");
        _agent.navMeshAgent.SetDestination(_target.transform.position);
        return;
      }

      _target = UnfinishedQuery.GetNeedingResources();

      if (_target == null) {
        Debug.Log("[DeliverUnfinished] No target needing resources");
        _abort = true;
      }
    }

    private void DeliverToTarget() {
      if (_target == null || _inventoryAgent == null) {
        _abort = true;
        return;
      }

      var needs = _target.GetRemainingResources();
      var targetInventory = _target.inventory;
      int delivered = 0;

      Debug.Log(
        $"Start delivering to {_target.name}. Required{string.Join(",", needs.Select(n => $"{n.tag}x{n.remaining}"))}",
        _target);
      foreach (var (tag, remaining) in needs) {
        if (!_inventoryAgent.inventory.TryGetSlotWithItemTags(new[] { tag }, out var slot)) {
          Debug.Log($"agent has no item with tag {tag}", _inventoryAgent.inventory);
          continue;
        }
        
        var count = slot.count;
        if (count <= remaining) {
          if (slot.Release(out var depositActor)) {
            if (targetInventory.TryPutItemInInventory(depositActor)) {
              Debug.Log($"Delivered full stack of {tag}", targetInventory);
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
        else {
          if (_creationModule.TrySpawnActorOnGround(slot.item.actorKey, _target.transform.position, out var newItem)) {
            newItem.GetStackData().current = remaining;
            if (targetInventory.TryPutItemInInventory(newItem, remaining)) {
              Debug.Log($"Delivered full stack of {tag} x {remaining}", targetInventory);
            }
            else {
              Debug.LogError($"Failed to deliver full stack of {tag} to target, destroying spawned item", _target);
              Object.Destroy(newItem.gameObject);
            }
          }
          else {
            Debug.LogError($"Failed to spawn newItem '{slot.item.actorKey}' for delivery");
          }

          slot.RemoveCount(remaining);
          delivered += remaining;
        }
      }

      Debug.Log($"Delivered total of {delivered} items to {_target.name}");
    }

    public override void OnStop() {
      _timer?.Dispose();
      _timer = null;
      _agent?.StopAndCleanPath();
      _target = null;
    }
  }
}
