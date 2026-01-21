using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.Animation;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class EatNearestStrategy : AgentStrategy {
    public float consumeDuration = 4f;
    
    private IGoapAgentCore _agent;
    private AnimationController _animations;
    private MemorySearcher _searcher;
    private SimTimer _timer;

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new EatNearestStrategy(agent) {
        consumeDuration = consumeDuration
      };
    }

    public EatNearestStrategy() {
      _searcher = new MemorySearcher() {
        requiredTags = new[] { Tag.FOOD }
      };
    }

    public EatNearestStrategy(IGoapAgentCore agent) : this() {
      _agent = agent;
      _animations = _agent.body?.animationController;
    }

    public override bool canPerform => !complete;
    public override bool complete { get; internal set; }

    public ActorDescription target { get; private set; }

    public override void OnStart() {
      complete = false;
      InitTimer();
      _searcher ??= new MemorySearcher() {
        requiredTags = new[] { Tag.FOOD }
      };

      var foodMemory = _searcher.GetNearest(_agent);
      if (foodMemory == null) {
        Debug.LogWarning("[EatNearest] No food memory, abort");
        _timer?.Stop();
        complete = true;
        return;
      }

      target = foodMemory.target;
      _timer.Start();
      _animations?.Eat();
      ApplyPerStatTick();
    }

    private void ApplyPerStatTick(float multiplier = 1f) {
      var descriptor = target;
      if (descriptor == null) return;
      _agent.body.AdjustStatPerTickDelta(descriptor.descriptionData.onUseAddStatPerTick, multiplier);
    }

    private void InitTimer() {
      _timer?.Dispose();
      _timer = new SimTimer(consumeDuration);
      _timer.OnTimerComplete += () => complete = true;
    }

    public override void OnStop() {
      _timer?.Dispose();
    }

    public override void OnComplete() {
      ApplyPerStatTick(-1);

      if (target != null) {
        Object.Destroy(target);
      }
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick(deltaTime);
    }
  }
}
