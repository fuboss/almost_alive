using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Descriptors;
using Content.Scripts.Animation;
using ImprovedTimers;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class PickupTransientStrategy : IActionStrategy {
    public float duration = 1f;
    private readonly AnimationController _animations;
    private readonly IGoapAgent _agent;
    private CountdownTimer _timer;

    public IActionStrategy Create(IGoapAgent agent) {
      return new PickupTransientStrategy(agent);
    }

    public PickupTransientStrategy() {
    }

    public PickupTransientStrategy(IGoapAgent agent) : this() {
      _agent = agent;
      _animations = _agent.animationController;
    }

    public bool canPerform => !complete && _agent?.transientTarget != null;
    public bool complete { get; private set; }

    public ActorDescription target { get; private set; }

    public void OnStart() {
      target = _agent?.transientTarget != null
        ? _agent.transientTarget.GetComponent<ActorDescription>()
        : null;
      if (target == null) {
        complete = true;
        Debug.LogError("Failed to pick up transient target, no ActorDescription found. Aborting PickupTransientStrategy");
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

    public void OnStop() {
      if (_timer != null && _timer.IsFinished) {
        if (target != null) {
          if (_agent.inventory.TryPutItemInInventory(target)) {
            UnityEngine.Debug.LogError($"{target.name} pickedUp!");
          }
        }
      }

      _timer?.Dispose();
    }

    public void OnUpdate(float deltaTime) {
      _timer.Tick();
    }
  }
}