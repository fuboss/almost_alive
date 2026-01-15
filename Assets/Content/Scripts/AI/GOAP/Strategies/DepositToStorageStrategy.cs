using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Animation;
using Content.Scripts.Game.Storage;
using ImprovedTimers;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class DepositToStorageStrategy : AgentStrategy {
    public float duration = 2f;
    private readonly AnimationController _animations;
    private readonly IGoapAgent _agent;
    private CountdownTimer _timer;

    public override IActionStrategy Create(IGoapAgent agent) {
      return new DepositToStorageStrategy(agent) {
        duration = duration
      };
    }

    public DepositToStorageStrategy() {
    }

    public DepositToStorageStrategy(IGoapAgent agent) : this() {
      _agent = agent;
      _animations = _agent.animationController;
    }

    public override bool canPerform => !complete && _agent?.transientTarget != null;
    public override bool complete { get; internal set; }

    public StorageActor target { get; private set; }

    public override void OnStart() {
      target = _agent?.transientTarget != null
        ? _agent.transientTarget.GetComponent<StorageActor>()
        : null;

      if (target == null) {
        complete = true;
        Debug.LogError(
          "Failed to pick up transient target, no ActorDescription found. Aborting PickupTransientStrategy");
        return;
      }

      IniTimer();
      _timer.Start();
      _animations.DepositItem();
    }

    private void IniTimer() {
      _timer?.Dispose();
      _timer = new CountdownTimer(duration); //animations.GetAnimationLength(animations.)

      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
    }

    public override void OnStop() {
      _timer?.Dispose();
      _agent.transientTarget = null;
    }

    public override void OnComplete() {
      if (target == null) return;
      var storage = target;
      if (_agent.inventory.TryGetSlotWithItemTags(storage.acceptedTags, out var slot)) {
        if (!slot.Release(out var item)) return;
        if (!storage.TryDeposit(item)) {
          _agent.inventory.TryPutItemInInventory(item);
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
      _timer.Tick();
    }
  }
}