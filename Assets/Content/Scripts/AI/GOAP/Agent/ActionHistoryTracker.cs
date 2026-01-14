using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Tracks action execution history for cooldown/variety calculations.
  /// Add to AgentBrain and call OnActionCompleted when actions finish.
  /// </summary>
  [Serializable]
  public class ActionHistoryTracker {
    [ShowInInspector, ReadOnly] 
    private Dictionary<string, float> _lastExecutionTime = new();
    
    [ShowInInspector, ReadOnly]
    private string _lastActionName;
    
    [ShowInInspector, ReadOnly]
    private int _consecutiveCount;

    public string lastActionName => _lastActionName;
    public int consecutiveCount => _consecutiveCount;

    public void OnActionCompleted(string actionName) {
      if (string.IsNullOrEmpty(actionName)) return;
      
      _lastExecutionTime[actionName] = Time.time;
      
      if (_lastActionName == actionName) {
        _consecutiveCount++;
      }
      else {
        _consecutiveCount = 1;
        _lastActionName = actionName;
      }
    }

    public float GetTimeSinceLastExecution(string actionName) {
      if (_lastExecutionTime.TryGetValue(actionName, out var lastTime)) {
        return Time.time - lastTime;
      }
      return float.MaxValue; // never executed
    }

    public bool WasExecutedWithin(string actionName, float seconds) {
      return GetTimeSinceLastExecution(actionName) <= seconds;
    }

    public void Clear() {
      _lastExecutionTime.Clear();
      _lastActionName = null;
      _consecutiveCount = 0;
    }
  }
}
