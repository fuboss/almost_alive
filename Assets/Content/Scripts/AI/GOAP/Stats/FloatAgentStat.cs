using System;

namespace Content.Scripts.AI.GOAP.Stats {
  [Serializable]
  public class FloatAgentStat : AgentStat<float> {
    public override float Normalized => maxValue == 0f ? 0f : value / maxValue;
    public FloatAgentStat(StatType type, float initialValue, float maxValue) 
      : base(type, initialValue, maxValue) {
    }
    
    [Serializable]
    public class Data {
      public StatType statType;
      public float minValue;
      public float maxValue;

      public Data Copy() {
        return new Data() {
          minValue = minValue,
          maxValue = maxValue,
          statType = statType
        };
      }
    }

    public FloatAgentStat Clone() {
      return new FloatAgentStat(type, value, maxValue);
    }
  }
}