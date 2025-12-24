using UnityEngine;
using UnityEngine.AI;
using UnityUtils;

namespace _Content.Scripts.AI.GOAP.Strategies {
  public class WanderStrategy : IActionStrategy {
    private readonly NavMeshAgent _agent;
    private readonly float _wanderRadius;

    public WanderStrategy(NavMeshAgent agent, float wanderRadius) {
      _agent = agent;
      _wanderRadius = wanderRadius;
    }

    public bool CanPerform => !Complete;
    public bool Complete => _agent.remainingDistance <= 2f && !_agent.pathPending;

    public void Start() {
      for (var i = 0; i < 5; i++) {
        var randomDirection = (Random.insideUnitSphere * _wanderRadius).With(y: 0);
        NavMeshHit hit;

        if (NavMesh.SamplePosition(_agent.transform.position + randomDirection, out hit, _wanderRadius, 1)) {
          _agent.SetDestination(hit.position);
          return;
        }
      }
    }
  }
}