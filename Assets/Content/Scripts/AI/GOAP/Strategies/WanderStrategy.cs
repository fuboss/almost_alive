using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;
using UnityEngine.AI;
using UnityUtils;

namespace Content.Scripts.AI.GOAP.Strategies {
  public class WanderStrategy : IActionStrategy {
    private readonly IGoapAgent _agent;
    private readonly float _wanderRadius;

    public WanderStrategy(IGoapAgent agent, float wanderRadius) {
      _agent = agent;
      _wanderRadius = wanderRadius;
    }

    public bool CanPerform => !Complete;
    public bool Complete => _agent.navMeshAgent.remainingDistance <= 2f && !_agent.navMeshAgent.pathPending;

    public void Start() {
      for (var i = 0; i < 5; i++) {
        var randomDirection = (Random.insideUnitSphere * _wanderRadius).With(y: 0);

        if (!NavMesh.SamplePosition(_agent.navMeshAgent.transform.position + randomDirection,
              out var hit, _wanderRadius, 1)) continue;
        _agent.navMeshAgent.SetDestination(hit.position);
        return;
      }
    }
  }
}