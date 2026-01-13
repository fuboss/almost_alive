using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent.Memory.Query;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent.Memory {
  [Serializable]
  public class MemoryConsolidationModule {
    [Title("Consolidation Settings")]
    [Range(0.9f, 0.999f)] 
    [Tooltip("Per-second confidence decay multiplier")]
    public float confidenceDecayRate = 0.99f;

    [Range(0f, 1f)]
    [Tooltip("Minimum confidence before forgetting")]
    public float forgetThreshold = 0.1f;

    [Range(0f, 30f)]
    [Tooltip("Seconds between reinforcement checks")]
    public float reinforcementInterval = 5f;

    [Range(0f, 1f)]
    [Tooltip("Confidence boost on reinforcement")]
    public float reinforcementBoost = 0.2f;

    [Title("Spatial Reinforcement")]
    public bool enableSpatialReinforcement = true;
    
    [ShowIf("enableSpatialReinforcement")]
    [Range(1f, 20f)]
    public float reinforcementRadius = 5f;

    [Title("Debug")]
    [ShowInInspector, ReadOnly] 
    private int _forgottenCount;
    
    [ShowInInspector, ReadOnly]
    private float _timeSinceReinforcement;

    private readonly Dictionary<MemorySnapshot, float> _reinforcementTimers = new();
    private readonly List<MemorySnapshot> _toForget = new();

    public void Tick(AgentMemory memory, float deltaTime, Vector3 agentPosition) {
      _timeSinceReinforcement += deltaTime;

      // Decay confidence
      foreach (var snapshot in memory.GetAllSnapshots()) {
        if (snapshot.IsExpired) continue;

        var decayAmount = 1f - Mathf.Pow(confidenceDecayRate, deltaTime);
        snapshot.confidence = Mathf.Max(0f, snapshot.confidence - decayAmount);

        if (snapshot.confidence < forgetThreshold) {
          _toForget.Add(snapshot);
        }
      }

      // Forget low-confidence memories
      foreach (var snapshot in _toForget) {
        memory.Forget(snapshot);
        _forgottenCount++;
      }
      _toForget.Clear();

      // Periodic reinforcement check
      if (_timeSinceReinforcement >= reinforcementInterval) {
        ProcessReinforcement(memory, agentPosition);
        _timeSinceReinforcement = 0f;
      }
    }

    private void ProcessReinforcement(AgentMemory memory, Vector3 agentPosition) {
      if (!enableSpatialReinforcement) return;

      var nearby = memory.Query()
        .InRadius(agentPosition, reinforcementRadius)
        .Where(s => !s.IsExpired)
        .Execute(memory);

      foreach (var snapshot in nearby) {
        ReinforceMemory(snapshot);
      }
    }

    public void ReinforceMemory(MemorySnapshot snapshot) {
      if (snapshot == null || snapshot.IsExpired) return;

      snapshot.confidence = Mathf.Min(1f, snapshot.confidence + reinforcementBoost);
      snapshot.lastUpdateTime = DateTime.UtcNow;
      
      _reinforcementTimers[snapshot] = 0f;
    }

    public void Reset() {
      _forgottenCount = 0;
      _timeSinceReinforcement = 0f;
      _reinforcementTimers.Clear();
    }

    public MemoryConsolidationStats GetStats() {
      return new MemoryConsolidationStats {
        ForgottenCount = _forgottenCount,
        TimeSinceReinforcement = _timeSinceReinforcement,
        TrackedMemories = _reinforcementTimers.Count
      };
    }
  }
}