using System;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Construction;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies.Construction {
  /// <summary>
  /// Works on construction site, adding progress.
  /// Completes when work is done and spawns the final actor.
  /// </summary>
  [Serializable]
  public class WorkOnConstructionStrategy : AgentStrategy {
    [Tooltip("Work units added per second")]
    public float workRate = 1f;

    private IGoapAgent _agent;
    private CampLocation _camp;
    private ConstructionSiteActor _site;
    private WorkState _state;

    public WorkOnConstructionStrategy() { }

    private WorkOnConstructionStrategy(IGoapAgent agent, WorkOnConstructionStrategy template) {
      _agent = agent;
      workRate = template.workRate;
    }

    public override bool canPerform => true;
    public override bool complete { get; internal set; }

    public override IActionStrategy Create(IGoapAgent agent) {
      return new WorkOnConstructionStrategy(agent, this);
    }

    public override void OnStart() {
      complete = false;
      _state = WorkState.FindSite;
    }

    public override void OnUpdate(float deltaTime) {
      switch (_state) {
        case WorkState.FindSite:
          FindSite();
          break;
        case WorkState.MovingToSite:
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

    private void FindSite() {
      _camp = _agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      
      // Find site that has all resources and needs work
      _site = ConstructionQuery.GetNeedingWork(_camp);

      if (_site == null) {
        // Maybe it's ready to complete?
        _site = ConstructionQuery.GetReadyToComplete(_camp);
        if (_site != null) {
          Debug.Log("[WorkConstruction] Found site ready to complete");
        }
      }

      if (_site == null) {
        Debug.Log("[WorkConstruction] No site ready for work");
        _state = WorkState.Done;
        return;
      }

      Debug.Log($"[WorkConstruction] Found site for {_site.recipe.recipeId}");
      _state = WorkState.MovingToSite;
      _agent.navMeshAgent.SetDestination(_site.transform.position);
    }

    private void UpdateMoving() {
      if (_site == null) {
        _state = WorkState.Done;
        return;
      }

      var nav = _agent.navMeshAgent;
      if (nav.pathPending) return;

      if (nav.remainingDistance <= 2f) {
        nav.ResetPath();
        _state = WorkState.Working;
        Debug.Log("[WorkConstruction] Started working");
      }
    }

    private void UpdateWorking(float deltaTime) {
      if (_site == null) {
        _state = WorkState.Done;
        return;
      }

      // Check if resources were removed
      if (!_site.hasAllResources) {
        Debug.LogWarning("[WorkConstruction] Resources missing, stopping");
        _state = WorkState.Done;
        return;
      }

      // Add work
      var workDone = _site.AddWork(workRate * deltaTime);

      if (workDone) {
        TryCompleteSite();
      }
    }

    private void TryCompleteSite() {
      var result = _site.TryComplete();
      if (result != null) {
        _agent.AddExperience(10);
        Debug.Log($"[WorkConstruction] Completed {result.actorKey}!");
      }
      
      _site = null;
      _state = WorkState.Done;
    }

    public override void OnStop() {
      _agent?.navMeshAgent?.ResetPath();
      _site = null;
      _camp = null;
    }

    private enum WorkState {
      FindSite,
      MovingToSite,
      Working,
      Done
    }
  }
}
