using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Animation;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class PickupTransientStrategy : AgentStrategy {
    public float duration = 1f;
    
    private IGoapAgentCore _agent;
    private ITransientTargetAgent _transientAgent;
    private IInventoryAgent _inventoryAgent;
    private UniversalAnimationController _animations;
    private SimTimer _timer;

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new PickupTransientStrategy(agent) {
        duration = duration
      };
    }

    public PickupTransientStrategy() {
    }

    public PickupTransientStrategy(IGoapAgentCore agent) : this() {
      _agent = agent;
      _transientAgent = agent as ITransientTargetAgent;
      _inventoryAgent = agent as IInventoryAgent;
      _animations = _agent.body?.animationController;
    }

    public override bool canPerform => !complete && _transientAgent?.transientTarget != null;
    public override bool complete { get; internal set; }

    public ActorDescription target { get; private set; }

    public override void OnStart() {
      complete = false;
      
      if (_transientAgent == null || _inventoryAgent == null) {
        complete = true;
        Debug.LogWarning("[PickupTransient] Agent missing ITransientTargetAgent or IInventoryAgent");
        return;
      }
      
      target = _transientAgent.transientTarget?.GetComponent<ActorDescription>();
      if (target == null) {
        complete = true;
        Debug.LogWarning("[PickupTransient] No target, abort");
        return;
      }

      InitTimer();
      _timer.Start();
      _animations?.PickUp();
    }

    private void InitTimer() {
      _timer?.Dispose();
      _timer = new SimTimer(duration);
      _timer.OnTimerComplete += () => complete = true;
    }

    public override void OnComplete() {
      if (target == null || _inventoryAgent == null || _transientAgent == null) return;
      
      if (_inventoryAgent.inventory.TryPutItemInInventory(target)) {
        _agent.memory.Forget(_transientAgent.transientTarget);
      }

      _transientAgent.transientTarget = null;
    }

    public override void OnStop() {
      _timer?.Dispose();
      _timer = null;
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick(deltaTime);
    }
  }
}
