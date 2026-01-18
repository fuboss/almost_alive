using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Core.Simulation;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class IdleStrategy : AgentStrategy {
    public float duration;
    private SimTimer _timer;

    public override IActionStrategy Create(IGoapAgent agent) {
      return new IdleStrategy(duration);
    }

    public IdleStrategy() {
    }

    private IdleStrategy(float duration) {
      this.duration = duration;
    }

    public override bool canPerform => true;
    public override bool complete { get; internal set; }

    public override void OnStart() {
      complete = false;
      _timer = new SimTimer(duration);
      _timer.OnTimerComplete += () => complete = true;
      _timer.Start();
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick(deltaTime);
    }

    public override void OnStop() {
      _timer?.Dispose();
      _timer = null;
    }
  }
}