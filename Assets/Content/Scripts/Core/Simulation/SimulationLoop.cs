using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Core.Simulation {
  public class SimulationLoop : ITickable {
    public const int TICKS_PER_SECOND = 30;
    public const float TICK_INTERVAL = 1f / TICKS_PER_SECOND;
    public const int MAX_TICKS_PER_FRAME = 10; // prevent spiral of death

    [Inject] private readonly SimulationTimeController _timeController;

    private readonly List<ISimulatable> _simulatables = new();
    private readonly List<ISimulatable> _pendingAdd = new();
    private readonly List<ISimulatable> _pendingRemove = new();
    private bool _isDirty;
    private float _accumulator;

    public int simulatableCount => _simulatables.Count;

    public void Register(ISimulatable simulatable) {
      if (simulatable == null || _simulatables.Contains(simulatable)) return;
      _pendingAdd.Add(simulatable);
      _isDirty = true;
    }

    public void Unregister(ISimulatable simulatable) {
      if (simulatable == null) return;
      _pendingRemove.Add(simulatable);
      _isDirty = true;
    }

    public void Tick() {
      if (_timeController.isPaused) return;

      ProcessPending();

      var scaledDelta = Time.deltaTime * _timeController.timeScale;
      _accumulator += scaledDelta;

      var ticksThisFrame = 0;
      while (_accumulator >= TICK_INTERVAL && ticksThisFrame < MAX_TICKS_PER_FRAME) {
        DoSimTick(TICK_INTERVAL);
        _accumulator -= TICK_INTERVAL;
        ticksThisFrame++;
      }

      // Prevent accumulator from growing indefinitely
      if (_accumulator > TICK_INTERVAL * MAX_TICKS_PER_FRAME) {
        _accumulator = 0f;
      }
    }

    private void DoSimTick(float simDeltaTime) {
      _timeController.AddSimTime(simDeltaTime);

      foreach (var sim in _simulatables) {
        sim.SimTick(simDeltaTime);
      }
    }

    private void ProcessPending() {
      if (!_isDirty) return;

      foreach (var s in _pendingRemove) {
        _simulatables.Remove(s);
      }
      _pendingRemove.Clear();

      foreach (var s in _pendingAdd) {
        if (!_simulatables.Contains(s)) {
          _simulatables.Add(s);
        }
      }
      _pendingAdd.Clear();

      // Sort by priority
      _simulatables.Sort((a, b) => a.tickPriority.CompareTo(b.tickPriority));

      _isDirty = false;
    }
  }
}
