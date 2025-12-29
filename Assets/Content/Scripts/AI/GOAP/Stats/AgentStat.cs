using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Core.Stats {
  public static class StatConstants {
    public const string HUNGER = "Hunger";
    public const string FATIGUE = "Fatigue";
    public const string SLEEP = "Sleep";
    public const string TOILET = "Toilet";
  }

  [Serializable]
  public abstract class AgentStat {
    [SerializeField] protected string _name;
    public string name => _name;
    public abstract float Normalized { get; }
  }

  [Serializable]
  [HideReferenceObjectPicker]
  public abstract class AgentStat<T> : AgentStat where T : IEquatable<T> {
    [SerializeField] private T _maxValue;
    [SerializeField] private T _value;
    public event Action OnChanged;

    public AgentStat(string name, T initialValue, T maxValue) {
      _value = initialValue;
      _maxValue = maxValue;
      _name = name;
    }

    public virtual T Value {
      get => _value;
      set {
        if (_value.Equals(value)) return;
        _value = value;
        OnStatChanged();
      }
    }


    public virtual T MaxValue {
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