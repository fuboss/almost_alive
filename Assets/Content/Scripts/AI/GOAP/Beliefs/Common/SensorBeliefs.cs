using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class VisionBelief : AgentBelief {
    [ValueDropdown("GetTags")] public string[] tags;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var sensor = agent.agentBrain.visionSensor;
        return sensor.HasObjectsWithTagsInView(tags);
      };
    }

    public override AgentBelief Copy() {
      return new VisionBelief() { condition = condition, name = name, tags = tags };
    }
  }
}