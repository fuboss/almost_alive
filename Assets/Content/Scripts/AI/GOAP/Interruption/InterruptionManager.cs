using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Goals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Interruption {
  /// <summary>
  /// Manages interruption sources and checks for plan interruptions.
  /// </summary>
  [Serializable]
  public class InterruptionManager {
    [Tooltip("Check for interruptions every N seconds (0 = every frame)")]
    [SerializeField] private float _checkInterval = 0.5f;
    
    [SerializeReference]
    [ListDrawerSettings(ShowFoldout = true)]
    private List<IInterruptionSource> _sources = new();

    private float _lastCheckTime;

    public IReadOnlyList<IInterruptionSource> sources => _sources;

    public void AddSource(IInterruptionSource source) {
      if (source == null) return;
      _sources.Add(source);
      SortSources();
    }

    public void RemoveSource(IInterruptionSource source) {
      _sources.Remove(source);
    }

    public void ClearSources() {
      _sources.Clear();
    }

    private void SortSources() {
      _sources = _sources.OrderByDescending(s => s.priority).ToList();
    }

    /// <summary>
    /// Check all sources for interruption. Returns true if should interrupt.
    /// </summary>
    public bool CheckForInterruption(
      IGoapAgent agent, 
      AgentGoal currentGoal,
      out AgentGoal interruptGoal,
      out bool shouldSavePlan) {
      
      interruptGoal = null;
      shouldSavePlan = false;

      // Interval check
      if (_checkInterval > 0 && Time.time - _lastCheckTime < _checkInterval) {
        return false;
      }
      _lastCheckTime = Time.time;

      // Check sources in priority order
      foreach (var source in _sources) {
        if (source == null) continue;
        
        if (source.ShouldInterrupt(agent, currentGoal, out interruptGoal)) {
          shouldSavePlan = source.shouldSaveCurrentPlan;
          Debug.Log($"[Interruption] {source.GetType().Name} triggered, goal: {interruptGoal?.Name}");
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Force immediate check (ignores interval).
    /// </summary>
    public bool ForceCheck(
      IGoapAgent agent,
      AgentGoal currentGoal,
      out AgentGoal interruptGoal,
      out bool shouldSavePlan) {
      
      _lastCheckTime = 0;
      return CheckForInterruption(agent, currentGoal, out interruptGoal, out shouldSavePlan);
    }
  }
}
