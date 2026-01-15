using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class InteractionBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var sensor = agent.agentBrain.interactSensor;
        var valid = sensor.HasObjectsWithTagsArea(tags);
        // if (!valid) {
        //   Debug.LogWarning($"Sensor has no objects with tags [{string.Join(", ", tags)}] in interaction zone");
        // }
        return valid;
      };
    }

    public override AgentBelief Copy() {
      return new InteractionBelief() { condition = condition, name = name, tags = tags };
    }
  }
}