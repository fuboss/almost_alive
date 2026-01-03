using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [CreateAssetMenu(fileName = "Belief", menuName = "GOAP/Belief", order = 0)]
  public class BeliefSO : SerializedScriptableObject {
    [SerializeReference] public AgentBelief belief;

    private void OnValidate() {
      if (belief != null) {
        belief.name = name;
      }
    }

    public AgentBelief Get(IGoapAgent agent) {
      return belief.Copy(agent);
    }
  }
}