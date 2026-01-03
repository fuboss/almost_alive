using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Descriptors;
using Content.Scripts.Animation;
using ImprovedTimers;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class UseRestPointStrategy : AgentStrategy {
    public float useDuration = 10;
    private readonly AnimationController _animations;
    private readonly IGoapAgent _agent;
    private CountdownTimer _timer;

    public override IActionStrategy Create(IGoapAgent agent) {
      return new UseRestPointStrategy(agent) {
        useDuration = useDuration
      };
    }

    public UseRestPointStrategy(){}
    public UseRestPointStrategy(IGoapAgent agent) {
      _agent = agent;
      _animations = _agent.animationController;
    }

    public override bool canPerform => !complete && _agent.transientTarget != null;

    public override bool complete { get; internal set; }

    public GameObject target { get; private set; }

    public override void OnStart() {
      IniTimer();

      _timer.Start();
      _agent.body.SetResting(true);

      target = _agent.transientTarget;
      Debug.LogError($"{_agent.navMeshAgent.name} start resting on {target.name}", target);

      //apply per-tick stat changes
      ApplyPerStatTick();
    }

    private void ApplyPerStatTick(float multiplier = 1f) {
      var descriptor = target.GetComponent<ActorDescription>();
      if (descriptor == null) return;
      _agent.body.AdjustStatPerTickDelta(descriptor.descriptionData.onUseAddStatPerTick, multiplier);
    }

    private void IniTimer() {
      _timer?.Dispose();
      _timer = new CountdownTimer(useDuration); //animations.GetAnimationLength(animations.)

      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
    }

    public override void OnStop() {
      //discard per-tick stat changes
      ApplyPerStatTick(-1);

      _agent.body.SetResting(false);
      _agent.transientTarget = null;
      _timer?.Dispose();
    }

    public override void OnUpdate(float deltaTime) {
      _timer.Tick();

      if (_agent.transientTarget == null) {
        complete = true;
      }
      //todo: terminate early if fatigue stat is full
    }
  }
}