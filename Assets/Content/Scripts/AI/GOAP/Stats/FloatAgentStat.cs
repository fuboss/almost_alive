using System;

namespace Content.Scripts.AI.GOAP.Core.Stats {
  [Serializable]
  public class FloatAgentStat : AgentStat<float> {
    public override float Normalized => MaxValue == 0f ? 0f : Value / MaxValue;
    public FloatAgentStat(string name, float initialValue, float maxValue) : base(name, initialValue, maxValue) {
    }
  }
}