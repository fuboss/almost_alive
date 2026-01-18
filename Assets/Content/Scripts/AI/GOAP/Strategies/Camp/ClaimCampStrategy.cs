using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using UnityEngine;
using VContainer;

namespace Content.Scripts.AI.GOAP.Strategies.Camp {
  /// <summary>
  /// Finds free CampLocation, moves to it, claims it, and instantiates CampSetup.
  /// </summary>
  [Serializable]
  public class ClaimCampStrategy : AgentStrategy {
    public float claimDuration = 2f;

    [Inject] private CampModule _campModule;

    private IGoapAgent _agent;
    private CampLocation _targetLocation;
    private SimTimer _claimTimer;
    private ClaimState _state;

    public ClaimCampStrategy() { }

    private ClaimCampStrategy(IGoapAgent agent, ClaimCampStrategy template) {
      _agent = agent;
      claimDuration = template.claimDuration;
      _campModule = template._campModule;
    }

    public override bool canPerform => _campModule != null && _campModule.isReady;
    public override bool complete { get; internal set; }

    public override IActionStrategy Create(IGoapAgent agent) {
      return new ClaimCampStrategy(agent, this);
    }

    public override void OnStart() {
      complete = false;
      _state = ClaimState.FindingLocation;
      InitTimer();
      FindCampLocation();
    }

    private void InitTimer() {
      _claimTimer?.Dispose();
      _claimTimer = new SimTimer(claimDuration);
      _claimTimer.OnTimerComplete += OnClaimComplete;
    }

    public override void OnUpdate(float deltaTime) {
      switch (_state) {
        case ClaimState.FindingLocation:
          // Handled in FindCampLocation
          break;
        case ClaimState.MovingToLocation:
          UpdateMoving();
          break;
        case ClaimState.Claiming:
          _claimTimer?.Tick(deltaTime);
          break;
        case ClaimState.Done:
          complete = true;
          break;
      }
    }

    private void FindCampLocation() {
      var locations = Registry<CampLocation>.GetAll();
      _targetLocation = locations
        .Where(l => !l.isClaimed)
        .OrderBy(l => Vector3.Distance(_agent.position, l.transform.position))
        .FirstOrDefault();

      if (_targetLocation == null) {
        Debug.LogWarning("[ClaimCamp] No free CampLocation found");
        _state = ClaimState.Done;
        return;
      }

      _state = ClaimState.MovingToLocation;
      _agent.navMeshAgent.SetDestination(_targetLocation.transform.position);
    }

    private void UpdateMoving() {
      if (_targetLocation == null || _targetLocation.isClaimed) {
        // Location taken while moving - find another
        FindCampLocation();
        return;
      }

      var nav = _agent.navMeshAgent;
      if (nav.pathPending) return;

      if (nav.remainingDistance <= 2f) {
        StartClaiming();
      }
    }

    private void StartClaiming() {
      if (_targetLocation == null || _targetLocation.isClaimed) {
        FindCampLocation();
        return;
      }

      _state = ClaimState.Claiming;
      _agent.navMeshAgent.ResetPath();
      _claimTimer.Start();
    }

    private void OnClaimComplete() {
      if (_targetLocation == null) {
        _state = ClaimState.Done;
        return;
      }

      // Try claim
      var goapAgent = _agent as GOAPAgent;
      if (goapAgent == null || !_targetLocation.TryClaim(goapAgent)) {
        Debug.LogWarning("[ClaimCamp] Failed to claim location");
        FindCampLocation();
        return;
      }

      // Instantiate setup
      var setup = _campModule.InstantiateRandomSetup(_targetLocation);
      if (setup == null) {
        Debug.LogError("[ClaimCamp] Failed to instantiate camp setup");
        _targetLocation.Release();
        _state = ClaimState.Done;
        return;
      }

      // Store in memory
      _agent.memory.persistentMemory.Remember(CampKeys.PERSONAL_CAMP, _targetLocation);
      Debug.Log($"[ClaimCamp] Claimed camp at {_targetLocation.name}");

      _state = ClaimState.Done;
    }

    public override void OnStop() {
      _claimTimer?.Dispose();
      _claimTimer = null;
      _agent?.navMeshAgent?.ResetPath();
      _targetLocation = null;
    }

    public override void OnComplete() {
      Debug.Log($"[ClaimCamp] Strategy complete. State: {_state}");
    }

    private enum ClaimState {
      FindingLocation,
      MovingToLocation,
      Claiming,
      Done
    }
  }
}
