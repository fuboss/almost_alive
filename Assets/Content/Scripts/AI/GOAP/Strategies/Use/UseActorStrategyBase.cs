using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies.Use {
  [Serializable]
  public abstract class UseActorStrategyBase : AgentStrategy {
    public float useDuration = 10;
    protected IGoapAgentCore agent;
    protected ITransientTargetAgent transientAgent;
    private SimTimer _timer;

    public override bool canPerform => !complete && transientAgent?.transientTarget != null;

    public override bool complete { get; internal set; }

    public ActorDescription target { get; private set; }

    public override void OnStart() {
      complete = false;
      
      if (transientAgent?.transientTarget == null) {
        Debug.LogWarning("[UseActorStrategy] No transient target");
        complete = true;
        return;
      }
      
      InitTimer();
      _timer.Start();

      target = transientAgent.transientTarget.GetComponent<ActorDescription>();
      ApplyOnStart();
    }


    protected void ApplyPerStatTick(float multiplier = 1f) {
      var descriptor = target?.GetComponent<ActorDescription>();
      if (descriptor == null) return;
      agent.body.AdjustStatPerTickDelta(descriptor.descriptionData.onUseAddStatPerTick, multiplier);
    }

    private void InitTimer() {
      _timer?.Dispose();
      _timer = new SimTimer(useDuration);
      _timer.OnTimerComplete += () => complete = true;
    }

    public override void OnStop() {
      ApplyOnStop();

      if (transientAgent != null) {
        transientAgent.transientTarget = null;
      }
      _timer?.Dispose();
    }

    protected virtual void ApplyOnStart() {
      ApplyPerStatTick();
    }

    protected virtual void ApplyOnStop() {
      ApplyPerStatTick(-1);
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick(deltaTime);

      if (transientAgent?.transientTarget == null) {
        complete = true;
      }
    }
  }
}
