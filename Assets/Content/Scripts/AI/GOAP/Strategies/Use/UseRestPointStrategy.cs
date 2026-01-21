using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Strategies.Use {
  [Serializable]
  public class UseRestPointStrategy : UseActorStrategyBase {
    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new UseRestPointStrategy(agent) {
        useDuration = useDuration
      };
    }

    public UseRestPointStrategy() {
    }

    public UseRestPointStrategy(IGoapAgentCore agent) {
      base.agent = agent;
      base.transientAgent = agent as ITransientTargetAgent;
    }

    protected override void ApplyOnStart() {
      base.ApplyOnStart();
      agent.body.SetResting(true);
    }

    protected override void ApplyOnStop() {
      base.ApplyOnStop();
      agent.body.SetResting(false);
    }
  }
}
