using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game.Craft;
using UnityEngine;
using VContainer;

namespace Content.Scripts.AI.GOAP.Strategies.Craft {
  [Serializable]
  public class DeliverToUnfinishedStrategy : AgentStrategy {
    public float duration = 2f;
    
    private IGoapAgentCore _agent;
    private ITransientTargetAgent _transientAgent;
    private IInventoryAgent _inventoryAgent;
    private IUnfinishedActor _target;
    private bool _abort;
    private SimTimer _timer;

    [Inject] protected ActorCreationModule _creationModule;
    private bool _complete;

    public DeliverToUnfinishedStrategy() {
    }

    private DeliverToUnfinishedStrategy(IGoapAgentCore agent, DeliverToUnfinishedStrategy template) {
      _agent = agent;
      _transientAgent = agent as ITransientTargetAgent;
      _inventoryAgent = agent as IInventoryAgent;
      duration = template.duration;
      _creationModule = template._creationModule;
    }

    public override bool canPerform => true;

    public override bool complete {
      get => _complete;
      internal set { _complete = value; }
    }

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new DeliverToUnfinishedStrategy(agent, this);
    }

    public override void OnStart() {
      complete = false;
      _abort = false;
      
      if (_inventoryAgent == null) {
        Debug.LogWarning("[DeliverUnfinished] Agent missing required interfaces");
        complete = true;
        return;
      }
      
      FindTarget();
      if (_target == null) {
        complete = true;
        return;
      }

      InitTimer();
    }

    private void InitTimer() {
      _timer?.Dispose();
      _timer = new SimTimer(duration);
      _timer.OnTimerComplete += () => complete = true;
      _timer.Start();
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick(deltaTime);
      if (_abort) {
        complete = true;
        _timer?.Stop();
        Debug.LogWarning("[DeliverUnfinished] Aborting delivery strategy");
      }
    }

    public override void OnComplete() {
      base.OnComplete();
      DeliverToTarget();
    }

    protected void FindTarget() {
      var unfinishedActor = _transientAgent?.transientTarget?.GetComponent<UnfinishedActorBase>();
      if (unfinishedActor != null) {
        _target = unfinishedActor;
        _agent.navMeshAgent.SetDestination(_target.actor.transform.position);
        return;
      }

      _target = UnfinishedQuery.GetNeedingResources();

      if (_target == null) {
        Debug.Log("[DeliverUnfinished] No target needing resources");
        _abort = true;
      }
    }

    private void DeliverToTarget() {
      if (_target == null || _inventoryAgent == null) {
        _abort = true;
        return;
      }

      var needs = _target.GetRemainingResources();
      var sourceInventory = _inventoryAgent.inventory;
      var targetInventory = _target.inventory;
      var totalDelivered = 0;

      foreach (var (tag, remaining) in needs) {
        var delivered = sourceInventory.TransferTo(targetInventory, tag, remaining, _creationModule);
        totalDelivered += delivered;
      }

      _target.CheckAllResourcesDelivered();
      Debug.Log($"[DeliverUnfinished] Delivered {totalDelivered} items to {_target.name}");
    }

    public override void OnStop() {
      _timer?.Dispose();
      _timer = null;
      _agent?.StopAndCleanPath();
      _target = null;
    }
  }
}
