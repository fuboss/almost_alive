using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;
using Content.Scripts.Animation;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Content.Scripts.AI.GOAP.Strategies.Use {
  [Serializable]
  public class ConsumeFromInventoryStrategy : AgentStrategy {
    public float consumeDuration = 4f;
    [ValueDropdown("GetTags")] public string[] tags;
    public int count = 1;
    
    private IGoapAgentCore _agent;
    private IInventoryAgent _inventoryAgent;
    private AnimationController _animations;
    private SimTimer _timer;
    private InventorySlot _slot;

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new ConsumeFromInventoryStrategy(agent) {
        consumeDuration = consumeDuration,
        tags = tags,
        count = count
      };
    }

    public ConsumeFromInventoryStrategy() {
    }

    public ConsumeFromInventoryStrategy(IGoapAgentCore agent) : this() {
      _agent = agent;
      _inventoryAgent = agent as IInventoryAgent;
      _animations = _agent.body?.animationController;
    }

    public override bool canPerform => !complete;
    public override bool complete { get; internal set; }

    public ActorDescription target { get; private set; }

    public override void OnStart() {
      complete = false;
      _slot = null;

      if (_inventoryAgent == null) {
        Debug.LogWarning("[ConsumeInventory] Agent missing IInventoryAgent");
        complete = true;
        return;
      }

      if (!_inventoryAgent.inventory.TryGetSlotWithItemTags(tags, out var slot)) {
        Debug.LogWarning("[ConsumeInventory] No matching item, abort");
        complete = true;
        return;
      }

      InitTimer();

      _slot = slot;
      target = slot.item;

      _timer.Start();
      _animations?.Eat();
      ApplyPerStatTick();
    }

    private void ApplyPerStatTick(float multiplier = 1f) {
      if (target == null) return;
      var descriptor = target.GetComponent<ActorDescription>();
      if (descriptor == null) return;
      _agent.body.AdjustStatPerTickDelta(descriptor.descriptionData.onUseAddStatPerTick, multiplier);
    }

    private void ApplyUseStat(float multiplier = 1f) {
      var descriptor = target?.GetComponent<ActorDescription>();
      if (descriptor?.descriptionData.onUseAddStats == null) return;

      foreach (var change in descriptor.descriptionData.onUseAddStats) {
        if (_agent.body.GetStat(change.statType) is FloatAgentStat stat) {
          stat.value += multiplier * Random.Range(change.minValue, change.maxValue);
        }
      }
    }

    private void InitTimer() {
      _timer?.Dispose();
      _timer = new SimTimer(consumeDuration);
      _timer.OnTimerComplete += () => complete = true;
    }

    public override void OnStop() {
      ApplyPerStatTick(-1);

      if (_timer != null && _timer.isComplete) {
        OnComplete();
      }

      _timer?.Dispose();
      _timer = null;
      _slot = null;
      target = null;
    }

    public override void OnComplete() {
      if (target == null || _slot == null) return;

      _slot.RemoveCount(count);
      ApplyUseStat();
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick(deltaTime);
    }
  }
}
