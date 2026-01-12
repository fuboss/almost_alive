using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;

namespace Content.Scripts.AI.GOAP.Beliefs.Stat {
  [Serializable]
  public class StatBelief : AgentBelief {
    public FloatAgentStat.Data statData = new();

    public override bool Evaluate(IGoapAgent agent) {
      condition = () => {
        var stat = agent.body.GetStat(statData.statType) as FloatAgentStat;
        return stat != null && stat.Normalized >= statData.minValue && stat.Normalized <= statData.maxValue;
      };

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy() {
      return new StatBelief { condition = condition, name = name, statData = statData.Copy() };
    }

    public override string ToString() {
      return base.ToString() + $"{statData.statType}";
    }
  }
}