using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;
using UnityEngine.AI;

namespace Content.Scripts.AI.GOAP.Strategies {
  public class MoveStrategy : IActionStrategy {
    private readonly IGoapAgent _agent;
    private readonly Func<Vector3> _destination;

    public MoveStrategy(IGoapAgent agent, Func<Vector3> destination) {
      _agent = agent;
      _destination = destination;
    }

    public MoveStrategy(IGoapAgent agent, MemorySnapshot snapshot) {
      _agent = agent;
      _destination = () => snapshot.location;
    }

    public bool CanPerform => !Complete;
    public bool Complete => _agent.navMeshAgent.remainingDistance <= 2f && !_agent.navMeshAgent.pathPending;

    public void Start() {
      _agent.navMeshAgent.SetDestination(_destination());
    }

    public void Stop() {
      _agent.navMeshAgent.ResetPath();
    }
  }
}