using System;
using Sirenix.OdinInspector;
using Content.Scripts.Core;

namespace Content.Scripts.Core.Simulation {
  public enum SimSpeed {
    PAUSED = 0,
    NORMAL = 1,
    FAST = 2,
    FASTEST = 3
  }

  [Serializable]
  public class SimulationTimeController {
    private static readonly float[] SPEED_MULTIPLIERS = { 0f, 1f, 2f, 3f };

    public static SimulationTimeController instance { get; private set; }

    [ShowInInspector, ReadOnly] private SimSpeed _currentSpeed = SimSpeed.NORMAL;
    [ShowInInspector, ReadOnly] private float _timeScale = 1f;
    [ShowInInspector, ReadOnly] private float _totalSimTime;

    public event Action<SimSpeed> OnSpeedChanged;

    static SimulationTimeController() {
      StaticResetRegistry.RegisterReset(() => instance = null);
    }

    public SimulationTimeController() {
      instance = this;
    }

    public SimSpeed currentSpeed => _currentSpeed;
    public float timeScale => _timeScale;
    public float totalSimTime => _totalSimTime;
    public bool isPaused => _currentSpeed == SimSpeed.PAUSED;

    public void SetSpeed(SimSpeed speed) {
      if (_currentSpeed == speed) return;
      _currentSpeed = speed;
      _timeScale = SPEED_MULTIPLIERS[(int)speed];
      OnSpeedChanged?.Invoke(_currentSpeed);
    }

    public void SetSpeed(int preset) {
      var clamped = Math.Clamp(preset, 0, SPEED_MULTIPLIERS.Length - 1);
      SetSpeed((SimSpeed)clamped);
    }

    public void TogglePause() {
      SetSpeed(_currentSpeed == SimSpeed.PAUSED ? SimSpeed.NORMAL : SimSpeed.PAUSED);
    }

    public void CycleSpeed() {
      var next = ((int)_currentSpeed + 1) % SPEED_MULTIPLIERS.Length;
      if (next == 0) next = 1;
      SetSpeed((SimSpeed)next);
    }

    public void AddSimTime(float delta) {
      _totalSimTime += delta;
    }
    
    public void SetSimTime(float simTime) {
      _totalSimTime = simTime;
    }
  }
}
