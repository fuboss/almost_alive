using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Goals;
using Content.Scripts.AI.GOAP.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Interruption {
  /// <summary>
  /// Interrupts when a stat falls below critical threshold.
  /// Example: very hungry, very tired.
  /// </summary>
  [Serializable]
  public class CriticalStatInterruption : InterruptionSourceBase {
    [SerializeField] private StatType _statType = StatType.HUNGER;
    
    [Tooltip("Interrupt when stat falls below this value (0-100)")]
    [Range(0, 100)]
    [SerializeField] private float _criticalThreshold = 15f;
    
    [Tooltip("Name of the goal to pursue when triggered")]
    [SerializeField] private string _goalName = "Eat";

    private AgentGoal _cachedGoal;

    protected override bool CheckInterruptCondition(IGoapAgent agent, out AgentGoal interruptGoal) {
      interruptGoal = null;
      
      var statValue = agent.body.GetStat(_statType);
      if (statValue.Normalized >= _criticalThreshold) return false;

      // Find the goal
      interruptGoal = FindGoal(agent);
      if (interruptGoal == null) {
        Debug.LogWarning($"[CriticalStatInterruption] Goal '{_goalName}' not found");
        return false;
      }

      Debug.Log($"[CriticalStatInterruption] {_statType} is critical ({statValue:F1} < {_criticalThreshold})");
      return true;
    }

    protected override bool IsAlreadyPursuingGoal(IGoapAgent agent, AgentGoal currentGoal) {
      if (currentGoal == null) return false;
      return currentGoal.Name == _goalName;
    }

    private AgentGoal FindGoal(IGoapAgent agent) {
      if (_cachedGoal != null && _cachedGoal.Name == _goalName) {
        return _cachedGoal;
      }

      var template = agent.agentBrain.goalTemplates?.FirstOrDefault(gt => gt.name == _goalName);
      if (template == null) return null;

      _cachedGoal = template.Get(agent);
      return _cachedGoal;
    }
  }
}
