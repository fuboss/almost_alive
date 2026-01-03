using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Stats {
  public static class StatConstants {
    public const string HUNGER = "HUNGER";
    public const string FATIGUE = "FATIGUE";
    public const string SLEEP = "SLEEP";
    public const string TOILET = "TOILET";
  }

  [Serializable]
  public abstract class AgentStat {
    [SerializeField] protected StatType _type;
    public StatType type => _type;
    public abstract float Normalized { get; }
  }

  [Serializable]
  [HideReferenceObjectPicker]
  public abstract class AgentStat<T> : AgentStat where T : IEquatable<T> {
    [SerializeField] private T _maxValue;
    [SerializeField] private T _value;
    public event Action OnChanged;

    public AgentStat(StatType type, T initialValue, T maxValue) {
      _value = initialValue;
      _maxValue = maxValue;
      _type = type;
    }

    public virtual T value {
      get => _value;
      set {
        if (_value.Equals(value)) return;
        _value = value;
        OnStatChanged();
      }
    }


    public virtual T maxValue {
      get => _maxValue;
      set {
        if (_maxValue.Equals(value)) return;
        _maxValue = value;
        OnStatChanged();
      }
    }

    private void OnStatChanged() {
      OnChanged?.Invoke();
    }
  }
}