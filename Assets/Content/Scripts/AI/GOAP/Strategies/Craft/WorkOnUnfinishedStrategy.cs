using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Core.Simulation;
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
    public float workRate = 2f;

    public float duration = 30;

    private IGoapAgentCore _agent;
    private ITransientTargetAgent _transientAgent;
    private IWorkAgent _workAgent;
    private UnfinishedActor _target;
    private SimTimer _timer;
    private bool _abort;

    public WorkOnUnfinishedStrategy() {
    }

    private WorkOnUnfinishedStrategy(IGoapAgentCore agent, WorkOnUnfinishedStrategy template) {
      _agent = agent;
      _transientAgent = agent as ITransientTargetAgent;
      _workAgent = (IWorkAgent)agent;
      workRate = template.workRate;
      duration = template.duration;
    }

    public override bool canPerform => true;
    public override bool complete { get; internal set; }

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new WorkOnUnfinishedStrategy(agent, this);
    }

    public override void OnStart() {
      complete = false;
      _abort = false;
      
      FindTarget();
      InitTimer();
      Debug.Log("[WorkUnfinished] Start working");
    }

    private void InitTimer() {
      _timer?.Dispose();
      _timer = new SimTimer(duration);
      _timer.OnTimerComplete += () => complete = true;
      _timer.Start();
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick(deltaTime);
      if (complete) return;
      if (_abort) {
        complete = true;
        _timer?.Stop();
        return;
      }

      UpdateWorking(deltaTime);
    }

    private void FindTarget() {
      var unfinishedActor = _transientAgent?.transientTarget?.GetComponent<UnfinishedActor>();
      if (unfinishedActor != null) {
        _target = unfinishedActor;
        Debug.Log($"[WorkUnfinished] Using transient target {_target.name}");
        _agent.navMeshAgent.SetDestination(_target.transform.position);
        return;
      }

      _target = UnfinishedQuery.GetNeedingWork();

      if (_target == null) {
        _target = UnfinishedQuery.GetReadyToComplete();
      }

      if (_target == null) {
        Debug.Log("[WorkUnfinished] No target ready for work");
        _abort = true;
        return;
      }

      Debug.Log($"[WorkUnfinished] Found {_target.recipe.recipeId}");
      _agent.navMeshAgent.SetDestination(_target.transform.position);
    }

    private void UpdateWorking(float deltaTime) {
      if (_target == null) {
        _abort = true;
        return;
      }

      if (!_target.hasAllResources) {
        Debug.LogWarning("[WorkUnfinished] Resources missing, stopping");
        _abort = true;
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
        _workAgent?.AddExperience(60); //todo move this to another place
        Debug.Log($"[WorkUnfinished] Completed {result.actorKey}!");
      }

      _timer.Stop();
      complete = true;
      _target = null;
    }

    public override void OnStop() {
      _agent?.navMeshAgent?.ResetPath();
      _timer?.Dispose();
      _timer = null;
      _target = null;
    }
  }
}
