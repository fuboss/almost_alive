using System;

namespace Content.Scripts.Core.Simulation {
  /// <summary>
  /// Timer that works with simulation time (respects pause/speed).
  /// Use Tick(deltaTime) with sim-scaled delta.
  /// </summary>
  public class SimTimer {
    private float _duration;
    private float _elapsed;
    private bool _isRunning;

    public event Action OnTimerStart;
    public event Action OnTimerStop;
    public event Action OnTimerComplete;

    public float duration => _duration;
    public float elapsed => _elapsed;
    public float remaining => Math.Max(0, _duration - _elapsed);
    public float progress => _duration > 0 ? Math.Clamp(_elapsed / _duration, 0f, 1f) : 1f;
    public bool isRunning => _isRunning;
    public bool isComplete => _elapsed >= _duration;

    public SimTimer(float duration) {
      _duration = duration;
      _elapsed = 0f;
      _isRunning = false;
    }

    public void Start() {
      _elapsed = 0f;
      _isRunning = true;
      OnTimerStart?.Invoke();
    }

    public void Start(float newDuration) {
      _duration = newDuration;
      Start();
    }

    public void Stop() {
      if (!_isRunning) return;
      _isRunning = false;
      OnTimerStop?.Invoke();
    }

    public void Reset() {
      _elapsed = 0f;
    }

    public void Reset(float newDuration) {
      _duration = newDuration;
      _elapsed = 0f;
    }

    /// <summary>
    /// Tick the timer. Returns true when timer completes this frame.
    /// </summary>
    public bool Tick(float deltaTime) {
      if (!_isRunning) return false;

      _elapsed += deltaTime;

      if (_elapsed >= _duration) {
        _isRunning = false;
        OnTimerComplete?.Invoke();
        OnTimerStop?.Invoke();
        return true;
      }

      return false;
    }

    public void Dispose() {
      OnTimerStart = null;
      OnTimerStop = null;
      OnTimerComplete = null;
    }
  }
}
