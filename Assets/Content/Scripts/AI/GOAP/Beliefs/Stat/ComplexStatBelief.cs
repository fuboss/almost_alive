using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;

namespace Content.Scripts.AI.GOAP.Beliefs.Stat {
  [Serializable]
  public class ComplexStatBelief : AgentBelief {
    public FloatAgentStat.Data[] statDatas;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var body = agent.body;
        foreach (var statData in statDatas) {
          if (body.GetStat(statData.statType) is not FloatAgentStat stat
              || stat.value < statData.minValue || stat.value > statData.maxValue) {
            return false;
          }
        }

        return true;
      };
    }

    public override AgentBelief Copy() {
      var copy = new ComplexStatBelief {
        condition = condition, name = name,
        statDatas = new FloatAgentStat.Data[statDatas.Length]
      };
      for (var i = 0; i < statDatas.Length; i++) {
        copy.statDatas[i] = statDatas[i].Copy();
      }

      return copy;
    }
  }
}