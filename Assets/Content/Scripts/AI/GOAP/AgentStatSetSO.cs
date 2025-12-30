using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP {
  [CreateAssetMenu(fileName = "StatSet", menuName = "GOAP/Agent Stat", order = 0)]
  public class AgentStatSetSO : SerializedScriptableObject {
    public List<FloatAgentStat> defaultStat = new() {
      new FloatAgentStat(StatType.HUNGER, 100f, 100f),
      new FloatAgentStat(StatType.FATIGUE, 0f, 100f),
      new FloatAgentStat(StatType.SLEEP, 100f, 100f),
      new FloatAgentStat(StatType.TOILET, 0f, 100f)
    };
    
    public Dictionary<StatType, float> defaultPerTickDelta = new() {
      { StatType.HUNGER, -4.5f },
      { StatType.FATIGUE, 0.5f },
      { StatType.SLEEP, -0.7f },
      { StatType.TOILET, 0.25f }
    };
  }
}