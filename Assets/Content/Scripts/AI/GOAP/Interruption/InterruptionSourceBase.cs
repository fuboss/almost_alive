using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Goals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Interruption {
  /// <summary>
  /// Base class for interruption sources. Can be used as SO or serialized in agent.
  /// </summary>
  [Serializable]
  public abstract class InterruptionSourceBase : IInterruptionSource {
    [SerializeField] protected float _priority = 10f;
    [SerializeField] protected bool _saveCurrentPlan = true;
    
    [Tooltip("Cooldown between interruption attempts")]
    [SerializeField] protected float _cooldown = 5f;
    
    [NonSerialized] private float _lastInterruptTime = -999f;

    public virtual float priority => _priority;
    public virtual bool shouldSaveCurrentPlan => _saveCurrentPlan;

    public bool ShouldInterrupt(IGoapAgent agent, AgentGoal currentGoal, out AgentGoal interruptGoal) {
      interruptGoal = null;
      
      // Cooldown check
      if (Time.time - _lastInterruptTime < _cooldown) return false;
      
      // Don't interrupt if already pursuing the interrupt goal
      if (IsAlreadyPursuingGoal(agent, currentGoal)) return false;
      
      // Check condition
      if (!CheckInterruptCondition(agent, out interruptGoal)) return false;
      
      _lastInterruptTime = Time.time;
      return true;
    }

    /// <summary>
    /// Override to check if interruption should happen.
    /// </summary>
    protected abstract bool CheckInterruptCondition(IGoapAgent agent, out AgentGoal interruptGoal);

    /// <summary>
    /// Override to prevent interrupting if already working on this goal type.
    /// </summary>
    protected virtual bool IsAlreadyPursuingGoal(IGoapAgent agent, AgentGoal currentGoal) {
      return false;
    }

    public void ResetCooldown() {
      _lastInterruptTime = -999f;
    }
  }
}
