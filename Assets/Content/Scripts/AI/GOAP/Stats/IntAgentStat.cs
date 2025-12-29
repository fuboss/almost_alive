using System;

namespace Content.Scripts.AI.GOAP.Core.Stats {
  [Serializable]
  public class IntAgentStat : AgentStat<int> {
    public override float Normalized => MaxValue == 0 ? 0f : (float)Value / MaxValue;

    public IntAgentStat(string name, int initialValue, int maxValue) : base(name, initialValue, maxValue) {
    }
  }
}