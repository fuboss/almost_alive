using System;
using Content.Scripts.Core.Simulation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Harvesting {
  /// <summary>
  /// Simulates fruit/resource growth on harvestable actors.
  /// Registers itself with SimulationLoop for automatic ticking.
  /// </summary>
  public class GrowthProgress : MonoBehaviour, ISimulatable {
    [ShowInInspector, ReadOnly, ProgressBar(0, 1)]
    private float _progress;

    [ShowInInspector, ReadOnly]
    private int _currentYield;

    private HarvestableTag _harvestableTag;
    private SimulationLoop _simulationLoop;
    private bool _isInitialized;

    public float progress => _progress;
    public int currentYield => _currentYield;
    public int maxYield => _harvestableTag?.maxHarvest ?? 0;
    public bool hasYield => _currentYield > 0;
    public int tickPriority => 100; // low priority, runs after agents

    public event Action<int> OnYieldChanged;

    public void Initialize(HarvestableTag tag, SimulationLoop simulationLoop) {
      _harvestableTag = tag;
      _simulationLoop = simulationLoop;
      _progress = 0f;
      _currentYield = 0;
      _isInitialized = true;
    }

    private void OnEnable() {
      if (_isInitialized) {
        _simulationLoop?.Register(this);
      }
      ActorRegistry<GrowthProgress>.Register(this);
    }

    private void OnDisable() {
      _simulationLoop?.Unregister(this);
      ActorRegistry<GrowthProgress>.Unregister(this);
    }

    public void SimTick(float simDeltaTime) {
      if (_harvestableTag == null || _currentYield >= maxYield) return;

      var respawnTime = _harvestableTag.respawnTime;
      if (respawnTime <= 0f) return;

      // Increment progress based on time
      var progressDelta = simDeltaTime / respawnTime;
      _progress = Mathf.Clamp01(_progress + progressDelta);

      // Calculate yield from curve
      var curveValue = _harvestableTag.respawnCurve.Evaluate(_progress);
      var newYield = Mathf.FloorToInt(curveValue * maxYield);

      if (newYield > _currentYield) {
        _currentYield = newYield;
        OnYieldChanged?.Invoke(_currentYield);
      }
    }

    /// <summary>
    /// Consume yield after harvest. Returns actual amount consumed.
    /// </summary>
    public int ConsumeYield(int amount) {
      var consumed = Mathf.Min(amount, _currentYield);
      _currentYield -= consumed;

      // Reset progress proportionally if all yield consumed
      if (_currentYield == 0) {
        _progress = 0f;
      }
      else {
        // Partial reset - find progress that matches current yield
        _progress = FindProgressForYield(_currentYield);
      }

      OnYieldChanged?.Invoke(_currentYield);
      return consumed;
    }

    private float FindProgressForYield(int targetYield) {
      // Binary search to find progress value that gives targetYield
      float lo = 0f, hi = 1f;
      for (int i = 0; i < 10; i++) {
        var mid = (lo + hi) / 2f;
        var curveValue = _harvestableTag.respawnCurve.Evaluate(mid);
        var yield = Mathf.FloorToInt(curveValue * maxYield);
        if (yield < targetYield) lo = mid;
        else hi = mid;
      }
      return lo;
    }

    /// <summary>
    /// Force set yield (for loading saves or debug).
    /// </summary>
    public void SetYield(int yield) {
      _currentYield = Mathf.Clamp(yield, 0, maxYield);
      _progress = FindProgressForYield(_currentYield);
      OnYieldChanged?.Invoke(_currentYield);
    }
  }
}
