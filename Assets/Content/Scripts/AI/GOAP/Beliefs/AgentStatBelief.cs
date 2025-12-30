using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;

namespace Content.Scripts.AI.GOAP.Beliefs {
  public class AgentStatBelief : AgentBelief {
    public StatType statType;
    public float minValue;
    public float maxValue;

    public override bool Evaluate(IGoapAgent agent) {
      if (_condition == null) {
        _condition = () => {
          var stat = (FloatAgentStat)agent.body.GetStat(statType);
          return stat.Value >= minValue && stat.Value <= maxValue;
        };
      }

      return base.Evaluate(agent);
    }
  }
}