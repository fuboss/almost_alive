using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Animation;
using ImprovedTimers;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies {
  public class EatNearestStrategy : IActionStrategy {
    private readonly AnimationController _animations;

    private readonly CountdownTimer _timer;
    private readonly IGoapAgent _agent;

    public EatNearestStrategy(IGoapAgent agent) {
      _agent = agent;
      _animations = _agent.animationController;
      _timer = new CountdownTimer(1f); //animations.GetAnimationLength(animations.)
      _timer.OnTimerStart += () => Complete = false;
      _timer.OnTimerStop += () => Complete = true;
    }

    public bool CanPerform => !Complete;
    public bool Complete { get; private set; }

    public void Start() {
      _timer.Start();
      _animations.Eat();
      var foodMemory = _agent.memory.GetNearest(
        _agent.position,
        new[] { "FOOD" },
        ms => ms.target != null
      );
      if (foodMemory == null) {
        _timer?.Stop();
        Complete = true;
        return;
      }

      var foodActor = foodMemory.target as GameObject;
      _agent.body.ConsumeFood(foodActor);
    }

    public void Update(float deltaTime) {
      _timer.Tick();
    }
  }
}