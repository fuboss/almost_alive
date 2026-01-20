using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Stat {
  [Serializable, TypeInfoBox("True when agent's stat is within specified range.")]
  public class StatBelief : AgentBelief {
    public FloatAgentStat.Data statData = new();

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var stat = agent.body.GetStat(statData.statType) as FloatAgentStat;
        return stat != null && stat.Normalized >= statData.minValue && stat.Normalized <= statData.maxValue;
      };
    }

    public override AgentBelief Copy() {
      return new StatBelief { name = name, statData = statData.Copy() };
    }

    public override string ToString() {
      return base.ToString() + $"{statData.statType}";
    }
  }
}
