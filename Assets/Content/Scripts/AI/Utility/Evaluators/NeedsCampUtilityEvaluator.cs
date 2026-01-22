using System;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.Utility {
  /// <summary>
  /// Returns high value when agent has no camp, zero when camp is established.
  /// </summary>
  [Serializable]
  public class NeedsCampUtilityEvaluator : EvaluatorBase {
    [Tooltip("Value returned when agent needs a camp")]
    public float noCampValue = 0.8f;

    public override float Evaluate(IGoapAgentCore agent) {
      if (agent is not ICampAgent campAgent) return 0f;
      
      var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      if (camp == null || !camp.hasSetup) return noCampValue;
      return 0f;
    }
  }
}
