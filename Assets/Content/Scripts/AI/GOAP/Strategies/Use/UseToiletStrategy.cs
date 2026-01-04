using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Strategies.Use {
  [Serializable]
  public class UseToiletStrategy : UseActorStrategyBase {
    public override IActionStrategy Create(IGoapAgent agent) {
      return new UseToiletStrategy(agent) {
        useDuration = useDuration
      };
    }

    public UseToiletStrategy() {
    }

    public UseToiletStrategy(IGoapAgent agent) {
      this.agent = agent;
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