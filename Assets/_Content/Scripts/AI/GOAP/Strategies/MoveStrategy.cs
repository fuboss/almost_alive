using System;
using UnityEngine;
using UnityEngine.AI;

namespace _Content.Scripts.AI.GOAP.Strategies {
  public class MoveStrategy : IActionStrategy {
    private readonly NavMeshAgent _agent;
    private readonly Func<Vector3> _destination;

    public MoveStrategy(NavMeshAgent agent, Func<Vector3> destination) {
      _agent = agent;
      _destination = destination;
    }

    public bool CanPerform => !Complete;
    public bool Complete => _agent.remainingDistance <= 2f && !_agent.pathPending;

    public void Start() {
      _agent.SetDestination(_destination());
    }

    public void Stop() {
      _agent.ResetPath();
    }
  }
}