using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
using Content.Scripts.AI.GOAP.Stats;
using Content.Scripts.Game;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies.Move {
  [Serializable]
  public class MoveStrategy : AgentStrategy {
    [SerializeField] public MemorySearcher targetFromMemory;
    public bool updateDestinationContinuously = false;

    public List<PerTickStatChange> statPerTick = new() {
      new PerTickStatChange() { statType = StatType.HUNGER, delta = -0.1f },
    };

    protected IGoapAgent _agent;
    protected Func<Vector3> _destination;
    protected MemorySnapshot _targetSnapshot;

    public MoveStrategy() {
    }

    public MoveStrategy(IGoapAgent agent, Func<Vector3> destination) {
      _agent = agent;
      _destination = destination;
    }

    public override bool canPerform => !complete;

    public override bool complete {
      get => _aborted || _agent.navMeshAgent.remainingDistance <= 2f && !_agent.navMeshAgent.pathPending;
      internal set { }
    }

    private bool _aborted;

    public override void OnStart() {
      _aborted = false;

      var targetMem = GetTargetMemory();
      if (targetMem == null) {
        Debug.LogError("Failed to get nearestTarget from memory");
        _aborted = true;
        return;
      }

      SetDestination(targetMem);
      UpdateAgentDestination();
      ApplyPerStatTick();
    }

    protected virtual MemorySnapshot GetTargetMemory() {
      return targetFromMemory?.GetNearest(_agent);
    }

    private void ApplyPerStatTick(float multiplier = 1f) {
      foreach (var change in statPerTick) {
        _agent.body.AdjustStatPerTickDelta(change.statType, multiplier * change.delta);
      }
    }

    public MoveStrategy SetDestination(MemorySnapshot snapshot) {
      _targetSnapshot = snapshot;
      _destination = () => _targetSnapshot.location;

      // Debug.Log($"MoveStrategy SetDestination to {snapshot.target.name} {snapshot.location}", snapshot.target);
      return this;
    }


    private void UpdateAgentDestination() {
      if (_destination != null) {
        _agent.navMeshAgent.SetDestination(_destination());
      }
      else {
        Debug.LogError("MoveStrategy has no destination set!");
      }
    }

    public override void OnComplete() {
      if (_targetSnapshot != null) {
        _agent.gameObject.transform.LookAt(_targetSnapshot.location, Vector3.up);
        _agent.transientTarget = _targetSnapshot.target;
        Debug.Log($"MoveStrategy OnComplete. to {_targetSnapshot.target.name} {_targetSnapshot.location}", _targetSnapshot.target);
      }
      _agent.navMeshAgent.ResetPath();
    }

    public override void OnStop() {
      ApplyPerStatTick(-1);

      _destination = null;
      _targetSnapshot = null;
    }


    public override void OnUpdate(float deltaTime) {
      //todo: use cooldown to avoid updating every frame
      if (updateDestinationContinuously) {
        UpdateAgentDestination();
      }
    }

    public override IActionStrategy Create(IGoapAgent agent) {
      var dest = _destination;
      if (targetFromMemory != null) {
        dest = () => targetFromMemory.Search(agent).location;
      }

      if (dest == null) {
        Debug.LogError("MoveStrategy Create: No destination set!");
        dest = () => agent.position;
      }

      return new MoveStrategy(agent, dest) {
        updateDestinationContinuously = updateDestinationContinuously,
        targetFromMemory = targetFromMemory
      };
    }
  }
}