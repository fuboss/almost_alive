using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Descriptors;
using Content.Scripts.AI.GOAP.Stats;
using Content.Scripts.Animation;
using ImprovedTimers;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class ConsumeFromInventoryStrategy : IActionStrategy {
    public float consumeDuration = 4f;
    public string[] tags;
    public int count = 1;
    private readonly AnimationController _animations;
    private readonly IGoapAgent _agent;
    private CountdownTimer _timer;
    private InventorySlot _slot;

    public IActionStrategy Create(IGoapAgent agent) {
      return new ConsumeFromInventoryStrategy(agent) {
        consumeDuration = consumeDuration,
        tags = tags,
        count = count
      };
    }

    public ConsumeFromInventoryStrategy() {
    }

    public ConsumeFromInventoryStrategy(IGoapAgent agent) : this() {
      _agent = agent;
      _animations = _agent.animationController;
    }

    public bool canPerform => !complete;
    public bool complete { get; private set; }

    public ActorDescription target { get; private set; }

    public void OnStart() {
      _slot = null;
     
      if (!_agent.inventory.TryGetItemWithTags(tags, out var slot)) {
        Debug.LogError("No matching item in inventory, Aborting ConsumeFromInventoryStrategy");
        _timer?.Stop();
        complete = true;
        return;
      }
      IniTimer();

      _slot = slot;
      target = slot.item;

      _timer.Start();
      _animations.Eat();
      //apply per-tick stat changes
      ApplyPerStatTick();
    }

    private void ApplyPerStatTick(float multiplier = 1f) {
      if (target == null) return;
      var descriptor = target.GetComponent<ActorDescription>();
      if (descriptor == null) return;
      if (descriptor.descriptionData.onUseAddStatPerTick == null) return;

      foreach (var change in descriptor.descriptionData.onUseAddStatPerTick) {
        _agent.body.AdjustStatPerTickDelta(change.statType, multiplier * change.delta);
      }
    }

    private void ApplyUseStat(float multiplier = 1f) {
      var descriptor = target.GetComponent<ActorDescription>();
      if (descriptor == null) return;
      if (descriptor.descriptionData.onUseAddStats == null) return;

      foreach (var change in descriptor.descriptionData.onUseAddStats) {
        if (_agent.body.GetStat(change.statType) is FloatAgentStat stat) {
          stat.value += multiplier * Random.Range(change.minValue, change.maxValue);
        }
      }
    }

    private void IniTimer() {
      _timer?.Dispose();
      _timer = new CountdownTimer(consumeDuration); //animations.GetAnimationLength(animations.)

      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
    }

    public void OnStop() {
      //discard per-tick stat changes
      ApplyPerStatTick(-1);
      
      if (_timer != null && _timer.IsFinished) {
        OnComplete();
      }

      _timer?.Dispose();
      _slot = null;
      target = null;
    }

    private void OnComplete() {
      if (target == null) return;
      
      //consume!
      ApplyUseStat();
      
      if (_slot.count > count) {
        _slot.stackData.current -= count;
      }
      else {
        _slot.Release(out var consumed);
        Object.Destroy(consumed.gameObject);
      }
    }

    public void OnUpdate(float deltaTime) {
      _timer.Tick();
    }
  }
}