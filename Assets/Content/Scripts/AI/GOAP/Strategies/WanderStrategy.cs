using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;
using UnityEngine.AI;
using UnityUtils;
using Random = UnityEngine.Random;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class WanderStrategy : IActionStrategy {
    public int navMeshSamples = 5;
    public float defaultWanderRadius = 10f;

    private IGoapAgent _agent;
    private Func<float> _wanderRadius;
    private Vector3? _targetPosition;

    public WanderStrategy() {
    }

    public WanderStrategy(IGoapAgent agent, Func<float> wanderRadius, Vector3? targetPosition = null) {
      _agent = agent;
      _wanderRadius = wanderRadius;
      _targetPosition = targetPosition;
    }

    public WanderStrategy Set(IGoapAgent agent, Func<float> wanderRadius, Vector3? targetPosition = null) {
      _agent = agent;
      _wanderRadius = wanderRadius;
      _targetPosition = targetPosition;
      return this;
    }

    public bool canPerform => !complete;
    public bool complete => _agent.navMeshAgent.remainingDistance <= 1f && !_agent.navMeshAgent.pathPending;

    public void Start() {
      var radius = _wanderRadius?.Invoke() ?? defaultWanderRadius;
      var targetPosition = _agent.position;
      var agentPosition = _targetPosition ?? _agent.position;

      for (var i = 0; i < navMeshSamples; i++) {
        var randomDirection = (Random.insideUnitSphere * radius).With(y: 0);
        if (!NavMesh.SamplePosition(agentPosition + randomDirection, out var hit, radius, 1)) continue;
        targetPosition = hit.position;
        break;
      }


      _agent.navMeshAgent.SetDestination(targetPosition);
    }

    public IActionStrategy Create(IGoapAgent agent) {
      return new WanderStrategy(agent, null);
    }
  }
}