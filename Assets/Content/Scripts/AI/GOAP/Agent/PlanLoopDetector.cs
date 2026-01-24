using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Detects when the same plan keeps failing repeatedly, indicating a loop.
  /// Tracks failed plans by their signature (goal + action sequence) and blocks them temporarily.
  /// </summary>
  [Serializable]
  public class PlanLoopDetector {
    [Tooltip("How many times a plan can fail before being blocked")]
    [SerializeField] private int _maxConsecutiveFailures = 3;
    
    [Tooltip("How long to block a failing plan (seconds)")]
    [SerializeField] private float _blockDuration = 30f;
    
    [ShowInInspector, ReadOnly]
    private Dictionary<string, int> _failureCounts = new();
    
    [ShowInInspector, ReadOnly]
    private Dictionary<string, float> _blockedUntil = new();
    
    [ShowInInspector, ReadOnly]
    private string _lastPlanSignature;

    /// <summary>
    /// Generates a unique signature for a plan based on goal and action sequence.
    /// </summary>
    public string GetPlanSignature(string goalName, IEnumerable<string> actionNames) {
      return $"{goalName}:{string.Join("→", actionNames)}";
    }

    /// <summary>
    /// Called when a plan fails (preconditions not met, stuck, etc.)
    /// Returns true if the plan should now be blocked.
    /// </summary>
    public bool OnPlanFailed(string planSignature) {
      if (string.IsNullOrEmpty(planSignature)) return false;

      _failureCounts.TryGetValue(planSignature, out var count);
      count++;
      _failureCounts[planSignature] = count;

      if (count >= _maxConsecutiveFailures) {
        BlockPlan(planSignature);
        Debug.LogError($"[PlanLoopDetector] Plan blocked due to repeated failures ({count}x): {planSignature}");
        return true;
      }

      if (planSignature == _lastPlanSignature) {
        Debug.LogWarning($"[PlanLoopDetector] Same plan failed again ({count}/{_maxConsecutiveFailures}): {planSignature}");
      }

      _lastPlanSignature = planSignature;
      return false;
    }

    /// <summary>
    /// Called when a plan succeeds. Resets failure count for that plan.
    /// </summary>
    public void OnPlanSucceeded(string planSignature) {
      if (string.IsNullOrEmpty(planSignature)) return;
      
      _failureCounts.Remove(planSignature);
      _lastPlanSignature = null;
    }

    /// <summary>
    /// Checks if a goal is currently blocked due to repeated plan failures.
    /// </summary>
    public bool IsGoalBlocked(string goalName) {
      // Check if any plan for this goal is blocked
      foreach (var kvp in _blockedUntil) {
        if (kvp.Key.StartsWith(goalName + ":") && Time.time < kvp.Value) {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Checks if a specific plan signature is blocked.
    /// </summary>
    public bool IsPlanBlocked(string planSignature) {
      if (_blockedUntil.TryGetValue(planSignature, out var blockedUntil)) {
        if (Time.time < blockedUntil) {
          return true;
        }
        // Block expired, remove it
        _blockedUntil.Remove(planSignature);
        _failureCounts.Remove(planSignature);
      }
      return false;
    }

    private void BlockPlan(string planSignature) {
      _blockedUntil[planSignature] = Time.time + _blockDuration;
    }

    /// <summary>
    /// Clears all tracking data. Call when agent state changes significantly.
    /// </summary>
    public void Clear() {
      _failureCounts.Clear();
      _blockedUntil.Clear();
      _lastPlanSignature = null;
    }

    /// <summary>
    /// Removes expired blocks. Call periodically for cleanup.
    /// </summary>
    public void PurgeExpiredBlocks() {
      var toRemove = new List<string>();
      foreach (var kvp in _blockedUntil) {
        if (Time.time >= kvp.Value) {
          toRemove.Add(kvp.Key);
        }
      }
      foreach (var key in toRemove) {
        _blockedUntil.Remove(key);
        _failureCounts.Remove(key);
      }
    }
  }
}

