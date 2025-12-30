using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Animation;
using ImprovedTimers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class EatNearestStrategy : IActionStrategy {
    public float consumeDuration = 4f;
    private readonly AnimationController _animations;
    private readonly IGoapAgent _agent;
    private MemorySearcher _searcher;
    private CountdownTimer _timer;

    public IActionStrategy Create(IGoapAgent agent) {
      return new EatNearestStrategy(agent);
    }

    public EatNearestStrategy() {
      _searcher = new MemorySearcher() {
        requiredTags = new[] { "FOOD" }
      };
    }

    public EatNearestStrategy(IGoapAgent agent) : this() {
      _agent = agent;
      _animations = _agent.animationController;
    }

    public bool canPerform => !complete;
    public bool complete { get; private set; }

    public GameObject target { get; private set; }

    public void Start() {
      IniTimer();
      _searcher ??= new MemorySearcher() {
        requiredTags = new[] { "FOOD" }
      };

      var foodMemory = _searcher.GetNearest(_agent);
      if (foodMemory == null) {
        Debug.LogError("food memory is null,Aborting EatNearestStrategy");
        _timer?.Stop();
        complete = true;
        return;
      }

      target = foodMemory.target;
      _timer.Start();
      _animations.Eat();
    }

    private void IniTimer() {
      _timer?.Dispose();
      _timer = new CountdownTimer(consumeDuration); //animations.GetAnimationLength(animations.)

      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
    }

    public void Stop() {
      if (_timer != null && _timer.IsFinished) {
        _agent.body.ConsumeFood(15);
        if (target != null) {
          Object.Destroy(target);
        }
      }

      _timer?.Dispose();
    }

    public void Update(float deltaTime) {
      _timer.Tick();
    }
  }
}