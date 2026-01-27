using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Animation;
using Content.Scripts.Game;
using Content.Scripts.Game.Harvesting;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Content.Scripts.AI.GOAP.Strategies {
  /// <summary>
  /// Strategy for harvesting yield from harvestable actors (berry bushes, crops, etc).
  /// Agent works on the target, and when work completes, yield drops on ground.
  /// </summary>
  [Serializable]
  public class HarvestStrategy : AgentStrategy {
    [Inject] private HarvestModule _harvestModule;

    [Tooltip("Work units added per second")]
    public float workRate = 2f;

    private IGoapAgentCore _agent;
    private ITransientTargetAgent _transientAgent;
    private UniversalAnimationController _animations;
    private HarvestingProgress _harvestingProgress;
    private GrowthProgress _growthProgress;

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new HarvestStrategy(agent) {
        workRate = workRate
      };
    }

    public HarvestStrategy() { }

    public HarvestStrategy(IGoapAgentCore agent) : this() {
      _agent = agent;
      _transientAgent = agent as ITransientTargetAgent;
      _animations = _agent.body?.animationController;
    }

    public override bool canPerform => !complete && _transientAgent?.transientTarget != null && HasYieldRemaining();
    public override bool complete { get; internal set; }

    public ActorDescription target { get; private set; }

    private bool HasYieldRemaining() {
      return _growthProgress != null && _growthProgress.hasYield;
    }

    public override void OnStart() {
      complete = false;

      if (_transientAgent == null) {
        complete = true;
        Debug.LogWarning("[Harvest] Agent missing ITransientTargetAgent");
        return;
      }

      target = _transientAgent.transientTarget?.GetComponent<ActorDescription>();
      if (target == null) {
        complete = true;
        Debug.LogWarning("[Harvest] No target, abort");
        return;
      }

      var harvestableTag = target.GetDefinition<HarvestableTag>();
      if (harvestableTag == null) {
        complete = true;
        Debug.LogWarning("[Harvest] No HarvestableTag on target, abort");
        return;
      }

      _growthProgress = target.GetComponent<GrowthProgress>();
      if (_growthProgress == null || !_growthProgress.hasYield) {
        complete = true;
        Debug.LogWarning("[Harvest] No yield available, abort");
        return;
      }

      _harvestingProgress = HarvestModule.GetOrCreateWorkProgress(target, harvestableTag.workPerUnit);

      _animations?.PickUp(); // TODO: proper harvest animation
      Debug.Log($"[Harvest] Started harvesting {target.name}, yield available: {_growthProgress.currentYield}");
    }

    public override void OnUpdate(float deltaTime) {
      if (_harvestingProgress == null || _growthProgress == null) return;

      // Check if still has yield
      if (!_growthProgress.hasYield) {
        complete = true;
        return;
      }

      // Add work
      var workDone = _harvestingProgress.AddWork(workRate * deltaTime);
      if (workDone) {
        // Harvest one unit
        if (_harvestModule.TryHarvestUnit(_harvestingProgress, _agent)) {
          // Check if more yield remains
          if (!_growthProgress.hasYield) {
            complete = true;
          }
          // Otherwise continue harvesting
        }
        else {
          complete = true;
        }
      }
    }

    public override void OnComplete() {
      if (target == null || _transientAgent == null) return;

      _agent.memory.Forget(_transientAgent.transientTarget);
      _transientAgent.transientTarget = null;

      Debug.Log($"[Harvest] Completed harvesting {target?.name}");
    }

    public override void OnStop() {
      _animations?.StopWork();
      _harvestingProgress = null;
      _growthProgress = null;
    }
  }
}
