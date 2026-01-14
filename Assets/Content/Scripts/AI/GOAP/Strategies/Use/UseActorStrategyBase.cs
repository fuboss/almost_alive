using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
using Content.Scripts.Game;
using ImprovedTimers;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies.Use {
  [Serializable]
  public abstract class UseActorStrategyBase : AgentStrategy {
    public float useDuration = 10;
    protected IGoapAgent agent;
    private CountdownTimer _timer;

    public override bool canPerform => !complete && agent.transientTarget != null;

    public override bool complete { get; internal set; }

    public ActorDescription target { get; private set; }

    public override void OnStart() {
      IniTimer();
      _timer.Start();

      target = agent.transientTarget.GetComponent<ActorDescription>();
      ApplyOnStart();
    }


    protected void ApplyPerStatTick(float multiplier = 1f) {
      var descriptor = target.GetComponent<ActorDescription>();
      if (descriptor == null) return;
      agent.body.AdjustStatPerTickDelta(descriptor.descriptionData.onUseAddStatPerTick, multiplier);
    }

    private void IniTimer() {
      _timer?.Dispose();
      _timer = new CountdownTimer(useDuration); //animations.GetAnimationLength(animations.)

      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
    }

    public override void OnStop() {
      //discard per-tick stat changes
      ApplyOnStop();

      agent.transientTarget = null;
      _timer?.Dispose();
    }

    protected virtual void ApplyOnStart() {
      ApplyPerStatTick();
    }

    protected virtual void ApplyOnStop() {
      ApplyPerStatTick(-1);
    }

    public override void OnUpdate(float deltaTime) {
      _timer.Tick();

      if (agent.transientTarget == null) {
        complete = true;
      }
      //todo: terminate early if fatigue stat is full
    }
  }
}