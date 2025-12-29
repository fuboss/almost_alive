using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Core.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Core {
  [CreateAssetMenu(fileName = "Stat", menuName = "GOAP/Agent Stat", order = 0)]
  public class AgentStatSO : SerializedScriptableObject {
    public FloatAgentStat defaultStat;
  }
}