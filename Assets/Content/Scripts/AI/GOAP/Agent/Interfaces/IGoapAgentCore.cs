using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Beliefs;
using UnityEngine;
using UnityEngine.AI;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Core interface for any GOAP-driven agent (human, animal, NPC).
  /// Provides navigation, brain, memory, and stats.
  /// </summary>
  public interface IGoapAgentCore {
    // Identity
    Transform transform { get; }
    GameObject gameObject { get; }

    // Navigation
    NavMeshAgent navMeshAgent { get; }
    Vector3 position => navMeshAgent.transform.position;
    Vector3 nextPosition => navMeshAgent.nextPosition;
    bool isMoving => navMeshAgent.velocity.sqrMagnitude > 0.01f && navMeshAgent.hasPath;
    void StopAndCleanPath();

    // Brain & Memory
    AgentBrain agentBrain { get; }
    AgentMemory memory => agentBrain.memory;

    AgentBelief GetBelief(string beliefName) {
      return agentBrain.Get(beliefName);
    }

    // Stats & Body
    AgentStatSetSO defaultStatSet { get; }
    AgentBody body { get; }
  }
}
