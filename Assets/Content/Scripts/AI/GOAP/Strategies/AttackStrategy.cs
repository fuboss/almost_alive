using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Animation;
using Content.Scripts.Core.Simulation;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class AttackStrategy : IActionStrategy {
    private readonly AnimationController _animations;
    private SimTimer _timer;

    public AttackStrategy(AnimationController animations) {
      _animations = animations;
    }

    public bool canPerform => true;
    public bool complete { get; private set; }

    public void OnStart() {
      complete = false;
      // TODO: Get duration from animation
      _timer = new SimTimer(1f);
      _timer.OnTimerComplete += () => complete = true;
      _timer.Start();
    }

    public void OnUpdate(float deltaTime) {
      _timer?.Tick(deltaTime);
    }

    public void OnStop() {
      _timer?.Dispose();
      _timer = null;
    }

    public void OnComplete() {
    }

    public IActionStrategy Create(IGoapAgent agent) {
      return new AttackStrategy(agent.animationController);
    }
  }
}