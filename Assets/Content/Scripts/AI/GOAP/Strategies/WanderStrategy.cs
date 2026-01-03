using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityUtils;
using Random = UnityEngine.Random;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class WanderUntilStrategy : WanderStrategy {
    [SerializeField] public MemorySearcher targetFromMemory;
    public int lookupForCountInMemory = 1;
    public Func<bool> stopCondition;

    public override bool complete {
      get => base.complete || (stopCondition != null && stopCondition.Invoke());
      internal set {
      }
    }

    public override void OnStart() {
      base.OnStart();
      stopCondition = () => { return _agent.memory.GetWithAllTags(new[] { "FOOD" }).Length >= lookupForCountInMemory; };
    }
  }


  [Serializable]
  public class WanderStrategy : AgentStrategy {
    [MinMaxSlider(1, 20)] public Vector2Int visitPointsMinMax = new(2, 5);
    public int navMeshSamples = 5;
    public float defaultWanderRadius = 10f;
    public bool debugWanderAroundCenter = true;

    protected IGoapAgent _agent;
    private Func<float> _wanderRadius;
    private int _visitedPoints= -1;
    private bool _aborted;
    private int _randPointsCount;

    public WanderStrategy() {
    }

    public WanderStrategy(IGoapAgent agent, Func<float> wanderRadius, Vector3? targetPosition = null) {
      _agent = agent;
      _wanderRadius = wanderRadius;
    }

    public WanderStrategy Set(IGoapAgent agent, Func<float> wanderRadius, Vector3? targetPosition = null) {
      _agent = agent;
      _wanderRadius = wanderRadius;
      return this;
    }

    public override bool canPerform => !complete;
    public override bool complete {
      get => _visitedPoints >= _randPointsCount && IsOnDesiredPosition();
      internal set {
      }
    }

    private bool IsOnDesiredPosition() {
      return _agent.navMeshAgent.remainingDistance <= 1f && !_agent.navMeshAgent.pathPending;
    }

    public override void OnStart() {
      _visitedPoints = 0;
      _randPointsCount = Random.Range(visitPointsMinMax.x, visitPointsMinMax.y + 1);
      _agent.navMeshAgent.SetDestination(PickNextWanderPosition());
    }

    private Vector3 PickNextWanderPosition() {
      var radius = _wanderRadius?.Invoke() ?? defaultWanderRadius;
      var aroundPosition = !debugWanderAroundCenter ? _agent.position : Vector3.up;
      var targetPosition = aroundPosition;

      for (var i = 0; i < navMeshSamples; i++) {
        var randomDirection = (Random.insideUnitSphere * radius).With(y: 0);
        if (!NavMesh.SamplePosition(aroundPosition + randomDirection, out var hit, radius, 1)) continue;
        targetPosition = hit.position;
        break;
      }

      return targetPosition;
    }

    public override void OnUpdate(float delta) {
      if (IsOnDesiredPosition()) {
        _visitedPoints++;
        _agent.navMeshAgent.SetDestination(PickNextWanderPosition());
//        Debug.Log($"WanderStrategy:VisitedPoints:{_visitedPoints}/{_randPointsCount}", _agent.navMeshAgent);
      }
      //todo: adjust different stats like hunger, fun, ets
    }

    public override void OnStop() {
      Debug.Log("WanderStrategy:Stop", _agent.navMeshAgent);
      _visitedPoints = -1;
    }

    public override IActionStrategy Create(IGoapAgent agent) {
      return new WanderStrategy(agent, null);
    }
  }
}