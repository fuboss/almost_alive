using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.Animation;
using Content.Scripts.Game;
using Content.Scripts.Game.Work;
using UnityEngine;
using UnityEngine.AI;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP.Agent {
  public interface IGoapAgent : ITickable {
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
    ActorDescription transientTarget { get; set; }
    int transientTargetId { get; }
    GameObject gameObject { get; }

    Transform transform { get; }
    public AgentExperience experience { get; }
    public AgentRecipes recipes { get; }
    RecipeModule recipeModule { get; }

    /// <summary>
    /// Returns agent's personal camp from persistent memory. Cached per call.
    /// </summary>
    CampLocation camp => memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);

    WorkPriority GetWorkScheduler();
    void AddExperience(int amount);
    void StopAndCleanPath();
  }
}