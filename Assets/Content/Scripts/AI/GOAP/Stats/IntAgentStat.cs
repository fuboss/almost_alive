using System;

namespace Content.Scripts.AI.GOAP.Stats {
  [Serializable]
  public class IntAgentStat : AgentStat<int> {
    public override float Normalized => maxValue == 0 ? 0f : (float)value / maxValue;

    public IntAgentStat(StatType name, int initialValue, int maxValue)
      : base(name, initialValue, maxValue) {
    }
  }
}