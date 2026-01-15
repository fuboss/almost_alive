using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
using Content.Scripts.Animation;
using Content.Scripts.Game;
using ImprovedTimers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class EatNearestStrategy : AgentStrategy {
    public float consumeDuration = 4f;
    private readonly AnimationController _animations;
    private readonly IGoapAgent _agent;
    private MemorySearcher _searcher;
    private CountdownTimer _timer;

    public override IActionStrategy Create(IGoapAgent agent) {
      return new EatNearestStrategy(agent) {
        consumeDuration = consumeDuration
      };
    }

    public EatNearestStrategy() {
      _searcher = new MemorySearcher() {
        requiredTags = new[] { Tag.FOOD }
      };
    }

    public EatNearestStrategy(IGoapAgent agent) : this() {
      _agent = agent;
      _animations = _agent.animationController;
    }

    public override bool canPerform => !complete;
    public override bool complete { get; internal set; }

    public ActorDescription target { get; private set; }

    public override void OnStart() {
      IniTimer();
      _searcher ??= new MemorySearcher() {
        requiredTags = new[] { Tag.FOOD }
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
      //apply per-tick stat changes
      ApplyPerStatTick();
    }

    private void ApplyPerStatTick(float multiplier = 1f) {
      var descriptor = target;
      if (descriptor == null) return;
      _agent.body.AdjustStatPerTickDelta(descriptor.descriptionData.onUseAddStatPerTick, multiplier);
    }

    private void IniTimer() {
      _timer?.Dispose();
      _timer = new CountdownTimer(consumeDuration); //animations.GetAnimationLength(animations.)

      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
    }

    public override void OnStop() {
      _timer?.Dispose();
    }

    public override void OnComplete() {
      //discard per-tick stat changes
      ApplyPerStatTick(-1);

      if (target != null) {
        Object.Destroy(target);
      }
    }

    public override void OnUpdate(float deltaTime) {
      _timer.Tick();
    }
  }
}