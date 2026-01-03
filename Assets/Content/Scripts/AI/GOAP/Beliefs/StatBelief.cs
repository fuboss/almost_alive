using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class StatBelief : AgentBelief {
    public FloatAgentStat.Data statData = new();

    public override bool Evaluate(IGoapAgent agent) {
      _condition = () => {
        var stat = agent.body.GetStat(statData.statType) as FloatAgentStat;
        return stat != null && stat.Normalized >= statData.minValue && stat.Normalized <= statData.maxValue;
      };

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy(IGoapAgent agent) {
      return new StatBelief { _condition = _condition, name = name, statData = statData.Copy() };
    }

    public override string ToString() {
      return base.ToString() + $"{statData.statType}";
    }
  }

  [Serializable]
  public class ComplexStatBelief : AgentBelief {
    public FloatAgentStat.Data[] statDatas;

    public override bool Evaluate(IGoapAgent agent) {
      _condition ??= () => {
        var body = agent.body;
        foreach (var statData in statDatas) {
          if (body.GetStat(statData.statType) is not FloatAgentStat stat
              || stat.value < statData.minValue || stat.value > statData.maxValue) {
            return false;
          }
        }

        return true;
      };

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy(IGoapAgent agent) {
      var copy = new ComplexStatBelief {
        _condition = _condition, name = name,
        statDatas = new FloatAgentStat.Data[statDatas.Length]
      };
      for (var i = 0; i < statDatas.Length; i++) {
        copy.statDatas[i] = statDatas[i].Copy();
      }

      return copy;
    }
  }
}