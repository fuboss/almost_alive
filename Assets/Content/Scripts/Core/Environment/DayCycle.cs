using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Core.Environment {
  public enum DayPhase { Night, Dawn, Day, Dusk }

  [Serializable]
  public class DayCycle {
    private EnvironmentSetupSO _setup;

    [ShowInInspector, ReadOnly] private float _totalSimTime;
    [ShowInInspector, ReadOnly] private float _normalizedTime;
    [ShowInInspector, ReadOnly] private DayPhase _currentPhase;
    [ShowInInspector, ReadOnly] private int _dayCount;

    public event Action<DayPhase> OnPhaseChanged;
    public event Action<int> OnNewDay;

    public float normalizedTime => _normalizedTime;
    public DayPhase currentPhase => _currentPhase;
    public int dayCount => _dayCount;
    public float totalSimTime => _totalSimTime;

    // Read from SO at runtime for hot-reload
    private float dayLength => _setup != null ? _setup.dayLengthSeconds : 1440f;

    public float hourOfDay => _normalizedTime * 24f;
    public bool isNight => _currentPhase == DayPhase.Night;
    public bool isDaytime => _currentPhase == DayPhase.Day;

    public void Initialize(EnvironmentSetupSO setup) {
      _setup = setup;
      if (_setup != null) {
        _totalSimTime = _setup.startTimeAsSimSeconds;
        Update(_totalSimTime);
      }
    }

    public void Tick(float simDeltaTime) {
      _totalSimTime += simDeltaTime;
      Update(_totalSimTime);
    }

    private void Update(float totalTime) {
      var prevDay = _dayCount;
      _dayCount = Mathf.FloorToInt(totalTime / dayLength);
      _normalizedTime = (totalTime % dayLength) / dayLength;

      if (_dayCount > prevDay) OnNewDay?.Invoke(_dayCount);

      var newPhase = EvaluatePhase(_normalizedTime);
      if (newPhase != _currentPhase) {
        _currentPhase = newPhase;
        OnPhaseChanged?.Invoke(_currentPhase);
      }
    }

    private DayPhase EvaluatePhase(float t) {
      if (_setup == null) return DayPhase.Day;
      
      if (t < _setup.dawnStart) return DayPhase.Night;
      if (t < _setup.dayStart) return DayPhase.Dawn;
      if (t < _setup.duskStart) return DayPhase.Day;
      if (t < _setup.nightStart) return DayPhase.Dusk;
      return DayPhase.Night;
    }

    public float GetPhaseProgress() {
      if (_setup == null) return 0f;
      
      return _currentPhase switch {
        DayPhase.Dawn => Mathf.InverseLerp(_setup.dawnStart, _setup.dayStart, _normalizedTime),
        DayPhase.Day => Mathf.InverseLerp(_setup.dayStart, _setup.duskStart, _normalizedTime),
        DayPhase.Dusk => Mathf.InverseLerp(_setup.duskStart, _setup.nightStart, _normalizedTime),
        DayPhase.Night => _normalizedTime < _setup.dawnStart
          ? Mathf.InverseLerp(_setup.nightStart - 1f, _setup.dawnStart, _normalizedTime - 1f)
          : Mathf.InverseLerp(_setup.nightStart, 1f + _setup.dawnStart, _normalizedTime),
        _ => 0f
      };
    }

    // Debug: set time directly
    public void SetTime(float normalizedTime) {
      _totalSimTime = normalizedTime * dayLength + _dayCount * dayLength;
      Update(_totalSimTime);
    }
  }
}
