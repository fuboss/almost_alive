using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Animation;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game.Storage;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class DepositToStorageStrategy : AgentStrategy {
    public float duration = 2f;
    
    private IGoapAgentCore _agent;
    private ITransientTargetAgent _transientAgent;
    private IInventoryAgent _inventoryAgent;
    private AnimationController _animations;
    private SimTimer _timer;

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new DepositToStorageStrategy(agent) {
        duration = duration
      };
    }

    public DepositToStorageStrategy() {
    }

    public DepositToStorageStrategy(IGoapAgentCore agent) : this() {
      _agent = agent;
      _transientAgent = agent as ITransientTargetAgent;
      _inventoryAgent = agent as IInventoryAgent;
      _animations = _agent.body?.animationController;
    }

    public override bool canPerform => !complete && _transientAgent?.transientTarget != null;
    public override bool complete { get; internal set; }

    public StorageActor target { get; private set; }

    public override void OnStart() {
      complete = false;
      
      if (_transientAgent == null || _inventoryAgent == null) {
        complete = true;
        Debug.LogWarning("[DepositStorage] Agent missing ITransientTargetAgent or IInventoryAgent");
        return;
      }
      
      target = _transientAgent.transientTarget?.GetComponent<StorageActor>();
      if (target == null) {
        complete = true;
        Debug.LogWarning("[DepositStorage] No storage target, abort");
        return;
      }

      InitTimer();
      _timer.Start();
      _animations?.DepositItem();
    }

    private void InitTimer() {
      _timer?.Dispose();
      _timer = new SimTimer(duration);
      _timer.OnTimerComplete += () => complete = true;
    }

    public override void OnStop() {
      _timer?.Dispose();
      _timer = null;
      _agent?.StopAndCleanPath();
      if (_transientAgent != null) {
        _transientAgent.transientTarget = null;
      }
    }

    public override void OnComplete() {
      if (target == null || _inventoryAgent == null) return;
      
      var storage = target;
      if (_inventoryAgent.inventory.TryGetSlotWithItemTags(storage.acceptedTags, out var slot)) {
        if (!slot.Release(out var item)) return;
        if (!storage.TryDeposit(item)) {
          _inventoryAgent.inventory.TryPutItemInInventory(item);
          Debug.LogWarning($"Failed to deposit {item.name} to {storage.name}, inventory full or item not accepted");
        }
        else {
          Debug.Log($"Deposited {item.name} to {storage.name}", storage);
        }
      }
      else {
        Debug.LogError(
          $"failed to find item in agent inventory to deposit into storage. [{string.Join(", ", storage.acceptedTags)}]",
          storage);
      }
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick(deltaTime);
    }
  }
}
