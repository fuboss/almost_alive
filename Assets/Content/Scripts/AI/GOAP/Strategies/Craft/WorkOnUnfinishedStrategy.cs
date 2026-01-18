using System;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Craft;
using ImprovedTimers;
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

    private IGoapAgent _agent;
    private CampLocation _camp;
    private UnfinishedActor _target;
    private CountdownTimer _timer;
    private bool _abort;

    public WorkOnUnfinishedStrategy() {
    }

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
      FindTarget();
      IniTimer();
      Debug.LogError("START WORKING!!!");
    }

    private void IniTimer() {
      _timer?.Dispose();
      _timer = new CountdownTimer(duration); //animations.GetAnimationLength(animations.)

      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
      _timer.Start();
    }

    public override void OnUpdate(float deltaTime) {
      _timer?.Tick();
      if (complete) return;
      if (_abort) {
        complete = true;
        _timer?.Stop();
        return;
      }

      UpdateWorking(deltaTime);
    }

    private void FindTarget() {
      _camp = _agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);

      if (_agent.transientTarget.GetComponent<UnfinishedActor>() is { } unfinishedActor) {
        _target = unfinishedActor;
        Debug.Log($"[WorkUnfinished] Using transient target {_target.recipe.recipeId}");
        _agent.navMeshAgent.SetDestination(_target.transform.position);
        return;
      }

      // Find that has all resources and needs work
      _target = UnfinishedQuery.GetNeedingWork(_camp);

      if (_target == null) {
        // Maybe ready to complete?
        _target = UnfinishedQuery.GetReadyToComplete(_camp);
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
        _agent.AddExperience(10);
        Debug.Log($"[WorkUnfinished] Completed {result.actorKey}!");
      }

      _timer.Stop();
      complete = true;
      _target = null;
    }

    public override void OnStop() {
      _agent?.navMeshAgent?.ResetPath();
      _target = null;
      _camp = null;
    }
  }
}