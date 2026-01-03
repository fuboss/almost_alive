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
    private MemorySnapshot _targetSnapshot;

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


    public bool canPerform => !complete;
    public bool complete => _aborted || _agent.navMeshAgent.remainingDistance <= 2f && !_agent.navMeshAgent.pathPending;
    private bool _aborted;

    public void OnStart() {
      _aborted = false;

      if (targetFromMemory != null) {
        var targetMem = targetFromMemory.GetNearest(_agent);
        if (targetMem == null) {
          Debug.LogError("Failed to get nearestTarget from memory");
          _aborted = true;
          return;
        }

        SetDestination(targetMem);
      }

      UpdateAgentDestination();
    }

    public MoveStrategy SetDestination(MemorySnapshot snapshot) {
      _targetSnapshot = snapshot;
      _destination = () => _targetSnapshot.location;

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

    public void OnStop() {
      _agent.navMeshAgent.ResetPath();
      if (_targetSnapshot != null) {
        _agent.transientTarget = _targetSnapshot.target;
      }

      _destination = null;
      _targetSnapshot = null;
    }


    public void OnUpdate(float deltaTime) {
      //todo: use cooldown to avoid updating every frame
      if (updateDestinationContinuously) {
        UpdateAgentDestination();
      }
    }


    public MoveStrategy SetAgent(IGoapAgent agent) {
      _agent = agent;
      return this;
    }

    public MoveStrategy SetDestination(Func<Vector3> destination) {
      _destination = destination;
      return this;
    }


    public IActionStrategy Create(IGoapAgent agent) {
      var dest = _destination;
      if (targetFromMemory != null) {
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