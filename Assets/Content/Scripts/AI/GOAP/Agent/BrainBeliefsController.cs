using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  public class BrainBeliefsController : SerializedMonoBehaviour {
    [SerializeField] private AgentBrain _agentBrain;
    public Dictionary<string, AgentBelief> beliefs;

    public void SetupBeliefs(IGoapAgent agent) {
      beliefs = new Dictionary<string, AgentBelief>();
      var factory = new BeliefFactory(agent, beliefs);

      factory.AddBelief("Nothing", () => false);

      factory.AddBelief("AgentIdle", () => !agent.navMeshAgent.hasPath);
      factory.AddBelief("AgentMoving", () => agent.navMeshAgent.hasPath);
      // factory.AddBelief("AgentHealthLow", () => health < 30);
      // factory.AddBelief("AgentIsHealthy", () => health >= 50);
      // factory.AddBelief("AgentStaminaLow", () => stamina < 10);
      // factory.AddBelief("AgentIsRested", () => stamina >= 50);

      // factory.AddLocationBelief("AgentAtDoorOne", 3f, _doorOnePosition);
      // factory.AddLocationBelief("AgentAtDoorTwo", 3f, _doorTwoPosition);
      // factory.AddLocationBelief("AgentAtRestingPosition", 3f, _restingPosition);
      // factory.AddLocationBelief("AgentAtFoodShack", 3f, _foodShack);

      factory.AddSensorBelief("PlayerInChaseRange", _agentBrain.chaseSensor);
      factory.AddSensorBelief("PlayerInAttackRange", _agentBrain.attackSensor);
      factory.AddBelief("AttackingPlayer", () => false); // Player can always be attacked, this will never become true
    }

    private void OnValidate() {
      if (_agentBrain == null) _agentBrain = GetComponent<AgentBrain>();
    }

    private void Start() {
    }

    public AgentBelief Get(string beliefName) {
      return beliefs.GetValueOrDefault(beliefName);
    }
  }
}