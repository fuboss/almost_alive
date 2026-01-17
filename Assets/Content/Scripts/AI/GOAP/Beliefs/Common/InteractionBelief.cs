using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable, TypeInfoBox("True when objects with specified tags are in agent's interaction zone.")]
  public class InteractionBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var sensor = agent.agentBrain.interactSensor;
        return sensor.HasObjectsWithTagsArea(tags);
      };
    }

    public override AgentBelief Copy() {
      return new InteractionBelief { condition = condition, name = name, tags = tags };
    }
  }
}
