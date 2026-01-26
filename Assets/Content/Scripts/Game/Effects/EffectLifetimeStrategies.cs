using System;
using UnityEngine;

namespace Content.Scripts.Game.Effects {
  public class DurationLifetimeStrategy : IEffectLifetimeStrategy {
    private readonly float _duration;

    public DurationLifetimeStrategy(float duration) {
      _duration = duration;
    }

    public bool ShouldComplete(EffectHandle handle, float elapsed) => elapsed >= _duration;
    public void OnStart(EffectHandle handle) { }
    public void OnComplete(EffectHandle handle) { }
  }

  public class ParticleCompleteLifetimeStrategy : IEffectLifetimeStrategy {
    private ParticleSystem _particleSystem;

    public bool ShouldComplete(EffectHandle handle, float elapsed) {
      if (_particleSystem == null) return true;
      return !_particleSystem.isPlaying && _particleSystem.particleCount == 0;
    }

    public void OnStart(EffectHandle handle) {
      if (handle.instance != null) {
        _particleSystem = handle.instance.GetComponent<ParticleSystem>();
      }
    }

    public void OnComplete(EffectHandle handle) {
      _particleSystem = null;
    }
  }

  public class ConditionLifetimeStrategy : IEffectLifetimeStrategy {
    private readonly Func<bool> _condition;

    public ConditionLifetimeStrategy(Func<bool> condition) {
      _condition = condition;
    }

    public bool ShouldComplete(EffectHandle handle, float elapsed) => _condition?.Invoke() ?? true;
    public void OnStart(EffectHandle handle) { }
    public void OnComplete(EffectHandle handle) { }
  }

  public class ManualLifetimeStrategy : IEffectLifetimeStrategy {
    public bool isComplete { get; set; }

    public bool ShouldComplete(EffectHandle handle, float elapsed) => isComplete;
    public void OnStart(EffectHandle handle) { }
    public void OnComplete(EffectHandle handle) { }

    public void Complete() => isComplete = true;
  }

  public class CompositeLifetimeStrategy : IEffectLifetimeStrategy {
    private readonly IEffectLifetimeStrategy[] _strategies;
    private readonly bool _requireAll;

    public CompositeLifetimeStrategy(bool requireAll, params IEffectLifetimeStrategy[] strategies) {
      _strategies = strategies;
      _requireAll = requireAll;
    }

    public bool ShouldComplete(EffectHandle handle, float elapsed) {
      if (_requireAll) {
        foreach (var s in _strategies) {
          if (!s.ShouldComplete(handle, elapsed)) return false;
        }
        return true;
      }

      foreach (var s in _strategies) {
        if (s.ShouldComplete(handle, elapsed)) return true;
      }
      return false;
    }

    public void OnStart(EffectHandle handle) {
      foreach (var s in _strategies) s.OnStart(handle);
    }

    public void OnComplete(EffectHandle handle) {
      foreach (var s in _strategies) s.OnComplete(handle);
    }
  }
}
