using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using ImprovedTimers;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class IdleStrategy : AgentStrategy {
    public float duration;
    private CountdownTimer _timer;

    public override IActionStrategy Create(IGoapAgent agent) {
      return new IdleStrategy(duration);
    }

    public IdleStrategy() {
      InitTimer(duration);
    }

    public IdleStrategy(float duration) {
      InitTimer(duration);
    }

    private void InitTimer(float d) {
      _timer = new CountdownTimer(d);
      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
    }

    public override bool canPerform => true; // Agent can always Idle
    public override bool complete { get; internal set; }

    public override void OnStart() {
      if (_timer == null) {
        InitTimer(duration);
      }

      _timer!.Start();
    }

    public override void OnUpdate(float deltaTime) {
      _timer.Tick();
    }
  }
}