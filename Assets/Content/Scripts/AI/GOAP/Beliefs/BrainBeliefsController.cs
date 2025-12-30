using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  public class BrainBeliefsController : SerializedMonoBehaviour {
    [SerializeField] private AgentBrain _agentBrain;
    public Dictionary<string, AgentBelief> beliefs;

    public void SetupBeliefs(IGoapAgent agent) {
      beliefs = new Dictionary<string, AgentBelief>();
      var factory = new BeliefFactory(agent, beliefs);
      var agentBody = agent.body;
      
      //basic movement beliefs
      factory.AddBelief(AgentConstants.Nothing, () => false);
      factory.AddBelief(AgentConstants.Idle, () => !agent.navMeshAgent.hasPath);
      factory.AddBelief(AgentConstants.Moving, () => agent.navMeshAgent.hasPath);
      
      //stat based beliefs
      factory.AddStatBelief("AgentIsHungry", StatConstants.HUNGER, v => v < 0.3f);
      factory.AddStatBelief("AgentIsNotHungry", StatConstants.HUNGER, v => v > 0.65f);
      factory.AddStatBelief("AgentIsTired", StatConstants.FATIGUE, v => v > 0.65f);
      factory.AddStatBelief("AgentToiletCritical", StatConstants.TOILET, v => v > 0.8f);
      factory.AddStatBelief("AgentIsExhausted", StatConstants.SLEEP, v => v < 0.2f);
      factory.AddBelief("AgentFeelsGood", ()
        => agentBody.GetStat(StatConstants.SLEEP).Normalized > 0.7f
           && agentBody.GetStat(StatConstants.HUNGER).Normalized < 0.5f
           && agentBody.GetStat(StatConstants.FATIGUE).Normalized < 0.5f
           && agentBody.GetStat(StatConstants.TOILET).Normalized < 0.3f
      );
      factory.AddBelief("AgentFeelsNormal", ()
        => agentBody.GetStat(StatConstants.SLEEP).Normalized > 0.4f
           && agentBody.GetStat(StatConstants.HUNGER).Normalized < 0.5f
           && agentBody.GetStat(StatConstants.FATIGUE).Normalized < 0.7f
           && agentBody.GetStat(StatConstants.TOILET).Normalized < 0.6f
      );
      factory.AddStatBelief("AgentIsRested", StatConstants.FATIGUE, v => v < 0.25f);
      // factory.AddSensorBelief("PlayerInChaseRange", _agentBrain.chaseSensor);
      // factory.AddBelief("AttackingPlayer", () => false); // Player can always be attacked, this will never become true
      //vision based beliefs
      factory.AddVisionBelief("AgentSeeFood", new[] { "Food" });
      factory.AddVisionBelief("AgentSeeOtherAgents", new[] { "Agent" });
      
      //memory based beliefs
      factory.AddMemoryBelief("RemembersFoodNearby", new[] { "Food" });
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