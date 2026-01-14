using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.Animation;
using UnityEngine;
using UnityEngine.AI;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP.Agent {
  public interface IGoapAgent: ITickable {
    AgentStatSetSO defaultStatSet { get; }
    AgentBrain agentBrain { get; }
    AgentMemory memory => agentBrain.memory;
    NavMeshAgent navMeshAgent { get; }
    AgentBody body { get; }
    Rigidbody rigidbody { get; }
    AnimationController animationController { get; }
    ActorInventory inventory { get; }
    public Vector3 position => navMeshAgent.transform.position;
    public Vector3 nextPosition => navMeshAgent.nextPosition;

    public AgentBelief GetBelief(string beliefName) {
      return agentBrain.Get(beliefName);
    }

    public bool isMoving => navMeshAgent.velocity.sqrMagnitude > 0.01f && navMeshAgent.hasPath;
    GameObject transientTarget { get; set; }
    GameObject gameObject { get; }
  }
}