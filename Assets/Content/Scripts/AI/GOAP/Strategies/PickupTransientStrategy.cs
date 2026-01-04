using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
using Content.Scripts.Animation;
using Content.Scripts.Game;
using ImprovedTimers;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class PickupTransientStrategy : AgentStrategy {
    public float duration = 1f;
    private readonly AnimationController _animations;
    private readonly IGoapAgent _agent;
    private CountdownTimer _timer;

    public override IActionStrategy Create(IGoapAgent agent) {
      return new PickupTransientStrategy(agent);
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
      target = _agent?.transientTarget != null
        ? _agent.transientTarget.GetComponent<ActorDescription>()
        : null;
      if (target == null) {
        complete = true;
        Debug.LogError(
          "Failed to pick up transient target, no ActorDescription found. Aborting PickupTransientStrategy");
        return;
      }

      IniTimer();
      _timer.Start();
      _animations.PickUp();
    }

    private void IniTimer() {
      _timer?.Dispose();
      _timer = new CountdownTimer(duration); //animations.GetAnimationLength(animations.)

      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
    }

    public override void OnStop() {
      if (_timer != null && _timer.IsFinished) {
        if (target != null && _agent.inventory.TryPutItemInInventory(target)) {
          Debug.Log($"{target.name} pickedUp!");
        }
      }

      _timer?.Dispose();
    }

    public override void OnUpdate(float deltaTime) {
      _timer.Tick();
    }
  }
}