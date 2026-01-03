using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class VisionBelief : AgentBelief {
    [ValueDropdown("GetTags")]public string[] tags;

    public override bool Evaluate(IGoapAgent agent) {
      var sensor = agent.agentBrain.visionSensor;
      _condition = () => sensor.HasObjectsWithTagsInView(tags);

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy(IGoapAgent agent) {
      return new VisionBelief() { _condition = _condition, name = name, tags = tags };
    }
  }

  [Serializable]
  public class InteractionBelief : AgentBelief {
    [ValueDropdown("GetTags")]public string[] tags;

    public override bool Evaluate(IGoapAgent agent) {
      var sensor = agent.agentBrain.interactSensor;
      _condition = () => sensor.HasObjectsWithTagsArea(tags);

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy(IGoapAgent agent) {
      return new InteractionBelief() { _condition = _condition, name = name, tags = tags };
    }
  }
}