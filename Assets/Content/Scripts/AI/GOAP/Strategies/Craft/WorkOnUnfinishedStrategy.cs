using System;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Craft;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies.Craft {
  /// <summary>
  /// Works on unfinished actor, adding progress.
  /// Completes when work is done and spawns the final actor.
  /// </summary>
  [Serializable]
  public class WorkOnUnfinishedStrategy : AgentStrategy {
    [Tooltip("Work units added per second")]
    public float workRate = 1f;

    private IGoapAgent _agent;
    private CampLocation _camp;
    private UnfinishedActor _target;
    private WorkState _state;

    public WorkOnUnfinishedStrategy() { }

    private WorkOnUnfinishedStrategy(IGoapAgent agent, WorkOnUnfinishedStrategy template) {
      _agent = agent;
      workRate = template.workRate;
    }

    public override bool canPerform => true;
    public override bool complete { get; internal set; }

    public override IActionStrategy Create(IGoapAgent agent) {
      return new WorkOnUnfinishedStrategy(agent, this);
    }

    public override void OnStart() {
      complete = false;
      _state = WorkState.FindTarget;
    }

    public override void OnUpdate(float deltaTime) {
      switch (_state) {
        case WorkState.FindTarget:
          FindTarget();
          break;
        case WorkState.MovingToTarget:
          UpdateMoving();
          break;
        case WorkState.Working:
          UpdateWorking(deltaTime);
          break;
        case WorkState.Done:
          complete = true;
          break;
      }
    }

    private void FindTarget() {
      _camp = _agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      
      // Find that has all resources and needs work
      _target = UnfinishedQuery.GetNeedingWork(_camp);

      if (_target == null) {
        // Maybe ready to complete?
        _target = UnfinishedQuery.GetReadyToComplete(_camp);
      }

      if (_target == null) {
        Debug.Log("[WorkUnfinished] No target ready for work");
        _state = WorkState.Done;
        return;
      }

      Debug.Log($"[WorkUnfinished] Found {_target.recipe.recipeId}");
      _state = WorkState.MovingToTarget;
      _agent.navMeshAgent.SetDestination(_target.transform.position);
    }

    private void UpdateMoving() {
      if (_target == null) {
        _state = WorkState.Done;
        return;
      }

      var nav = _agent.navMeshAgent;
      if (nav.pathPending) return;

      if (nav.remainingDistance <= 2f) {
        nav.ResetPath();
        _state = WorkState.Working;
        Debug.Log("[WorkUnfinished] Started working");
      }
    }

    private void UpdateWorking(float deltaTime) {
      if (_target == null) {
        _state = WorkState.Done;
        return;
      }

      if (!_target.hasAllResources) {
        Debug.LogWarning("[WorkUnfinished] Resources missing, stopping");
        _state = WorkState.Done;
        return;
      }

      var workDone = _target.AddWork(workRate * deltaTime);

      if (workDone) {
        TryComplete();
      }
    }

    private void TryComplete() {
      var result = _target.TryComplete();
      if (result != null) {
        _agent.AddExperience(10);
        Debug.Log($"[WorkUnfinished] Completed {result.actorKey}!");
      }
      
      _target = null;
      _state = WorkState.Done;
    }

    public override void OnStop() {
      _agent?.navMeshAgent?.ResetPath();
      _target = null;
      _camp = null;
    }

    private enum WorkState {
      FindTarget,
      MovingToTarget,
      Working,
      Done
    }
  }
}
