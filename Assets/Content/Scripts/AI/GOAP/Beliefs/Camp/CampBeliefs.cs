using System;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs.Camp {
  [Serializable]
  public class HasCampBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.memoryK.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return camp != null && camp.hasSetup;
      };
    }

    public override AgentBelief Copy() => new HasCampBelief { name = name };
  }

  /// <summary>Inverse of HasCampBelief - true when agent needs a camp.</summary>
  [Serializable]
  public class NeedsCampBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.memoryK.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return camp == null || !camp.hasSetup;
      };
    }

    public override AgentBelief Copy() => new NeedsCampBelief { name = name };
  }
}
