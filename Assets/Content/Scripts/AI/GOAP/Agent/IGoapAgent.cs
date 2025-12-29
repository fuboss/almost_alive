using Content.Scripts.AI.GOAP.Core;
using Content.Scripts.Animation;
using UnityEngine;
using UnityEngine.AI;

namespace Content.Scripts.AI.GOAP.Agent {
  public interface IGoapAgent {
    AgentBrain agentBrain { get; }
    NavMeshAgent navMeshAgent { get; }
    AgentBody body { get; }
    Rigidbody rigidbody { get; }
    AnimationController animationController { get; }

    public Vector3 position => navMeshAgent.transform.position;
    public Vector3 nextPosition => navMeshAgent.nextPosition;
    
    public AgentBelief GetBelief(string beliefName) {
      return agentBrain.beliefsController.Get(beliefName);
    }
  }
}