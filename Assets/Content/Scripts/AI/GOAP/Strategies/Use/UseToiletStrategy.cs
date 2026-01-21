using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Strategies.Use {
  [Serializable]
  public class UseToiletStrategy : UseActorStrategyBase {
    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new UseToiletStrategy(agent) {
        useDuration = useDuration
      };
    }

    public UseToiletStrategy() {
    }

    public UseToiletStrategy(IGoapAgentCore agent) {
      this.agent = agent;
      this.transientAgent = agent as ITransientTargetAgent;
    }

    protected override void ApplyOnStart() {
      base.ApplyOnStart();
      agent.body.SetToiletUse(true);
    }

    protected override void ApplyOnStop() {
      base.ApplyOnStop();
      agent.body.SetToiletUse(false);
    }
  }
}
