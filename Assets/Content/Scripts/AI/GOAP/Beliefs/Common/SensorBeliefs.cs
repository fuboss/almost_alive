using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class VisionBelief : AgentBelief {
    [ValueDropdown("GetTags")]public string[] tags;

    public override bool Evaluate(IGoapAgent agent) {
      var sensor = agent.agentBrain.visionSensor;
      condition = () => sensor.HasObjectsWithTagsInView(tags);

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy() {
      return new VisionBelief() { condition = condition, name = name, tags = tags };
    }
  }

  [Serializable]
  public class InteractionBelief : AgentBelief {
    [ValueDropdown("GetTags")]public string[] tags;

    public override bool Evaluate(IGoapAgent agent) {
      var sensor = agent.agentBrain.interactSensor;
      condition = () => sensor.HasObjectsWithTagsArea(tags);

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy() {
      return new InteractionBelief() { condition = condition, name = name, tags = tags };
    }
  }
}