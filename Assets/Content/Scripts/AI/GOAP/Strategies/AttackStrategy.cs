using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Animation;
using Content.Scripts.Core.Simulation;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class AttackStrategy : AgentStrategy {
    private UniversalAnimationController _animations;
    private SimTimer _timer;

    public AttackStrategy() {
    }

    public AttackStrategy(IGoapAgentCore agent) {
      _animations = agent.body?.animationController;
    }

    public override bool canPerform => true;
    public override bool complete { get; internal set; }

    public override void OnStart() {
      complete = false;
      // TODO: Get duration from animation
      _timer = new SimTimer(1f);
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

    public override void OnComplete() {
    }

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new AttackStrategy(agent);
    }
  }
}
