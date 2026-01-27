using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Harvesting;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Harvesting {
  /// <summary>
  /// True when transient target is harvestable and has yield available.
  /// </summary>
  [Serializable, TypeInfoBox("True when transient target has harvestable yield available.")]
  public class HarvestableHasYieldBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      if (agent is not ITransientTargetAgent targetAgent) {
        return () => inverse;
      }

      return () => {
        var target = targetAgent.transientTarget;
        if (target == null) return inverse;

        var hasYield = HarvestModule.HasYield(target);
        return inverse ? !hasYield : hasYield;
      };
    }

    public override AgentBelief Copy() {
      return new HarvestableHasYieldBelief {
        inverse = inverse,
        name = name
      };
    }

    public override string GetPresenterString() {
      return inverse ? $"!{name}" : name;
    }
  }
}
