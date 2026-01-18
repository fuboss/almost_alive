using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Animation;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class PickupTransientStrategy : AgentStrategy {
    public float duration = 1f;
    private readonly AnimationController _animations;
    private readonly IGoapAgent _agent;
    private SimTimer _timer;

    public override IActionStrategy Create(IGoapAgent agent) {
      return new PickupTransientStrategy(agent) {
        duration = duration
      };
    }

    public PickupTransientStrategy() {
    }

    public PickupTransientStrategy(IGoapAgent agent) : this() {
      _agent = agent;
      _animations = _agent.animationController;
    }

    public override bool canPerform => !complete && _agent?.transientTarget != null;
    public override bool complete { get; internal set; }

    public ActorDescription target { get; private set; }

    public override void OnStart() {
      complete = false;
      target = _agent?.transientTarget != null
        ? _agent.transientTarget.GetComponent<ActorDescription>()
        : null;
      if (target == null) {
        complete = true;
        Debug.LogWarning("[PickupTransient] No target, abort");
        return;
      }

      InitTimer();
      _timer.Start();
      _animations?.PickUp();
    }

    private void InitTimer() {
      _timer?.Dispose();
      _timer = new SimTimer(duration);
      _timer.OnTimerComplete += () => complete = true;
    }

    public override void OnComplete() {
      if (target == null) return;
      if (_agent.inventory.TryPutItemInInventory(target)) {
        _agent.memory.Forget(_agent.transientTarget);
      }

      _agent.transientTarget = null;
    }

    public override void OnStop() {
      _timer?.Dispose();
      _timer = null;
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick(deltaTime);
    }
  }
}