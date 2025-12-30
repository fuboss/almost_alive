using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class MoveStrategy : IActionStrategy {
    [SerializeField] public MemorySearcher targetFromMemory;
    public bool updateDestinationContinuously = false;

    private IGoapAgent _agent;
    private Func<Vector3> _destination;

    public MoveStrategy() {
    }

    public MoveStrategy(IGoapAgent agent, Func<Vector3> destination) {
      _agent = agent;
      _destination = destination;
    }

    public MoveStrategy(IGoapAgent agent, Func<MemorySnapshot> snapshot) {
      _agent = agent;
      _destination = () => snapshot().location;
    }

    public MoveStrategy SetAgent(IGoapAgent agent) {
      _agent = agent;
      return this;
    }

    public MoveStrategy SetDestination(Func<Vector3> destination) {
      _destination = destination;
      return this;
    }

    public MoveStrategy SetDestination(Func<MemorySnapshot> snapshot) {
      _destination = () => snapshot().location;
      return this;
    }

    public bool canPerform => !complete;
    public bool complete => _agent.navMeshAgent.remainingDistance <= 2f && !_agent.navMeshAgent.pathPending;

    public void Start() {
      if (_destination == null && targetFromMemory != null) {
        var targetMem = targetFromMemory.GetNearest(_agent);
        if (targetMem != null) {
          _destination = () => targetMem.location;
        }
      }

      UpdateAgentDestination();
    }

    private void UpdateAgentDestination() {
      if (_destination != null) {
        _agent.navMeshAgent.SetDestination(_destination());
      }
      else {
        Debug.LogError("MoveStrategy has no destination set!");
      }
    }

    public void Stop() {
      _agent.navMeshAgent.ResetPath();
    }

    public void Update(float deltaTime) {
      //todo: use cooldown to avoid updating every frame
      if (updateDestinationContinuously) {
        UpdateAgentDestination();
      }
    }

    public IActionStrategy Create(IGoapAgent agent) {
      var dest = _destination;
      if (dest == null && targetFromMemory != null) {
        dest = targetFromMemory.Search(agent);
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