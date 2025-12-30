using Content.Scripts.AI.GOAP.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP {
  [CreateAssetMenu(fileName = "Stat", menuName = "GOAP/Agent Stat", order = 0)]
  public class AgentStatSO : SerializedScriptableObject {
    public FloatAgentStat defaultStat;
  }
}