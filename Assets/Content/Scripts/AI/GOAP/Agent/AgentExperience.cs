using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  [Serializable]
  public class AgentExperience {
    [ShowInInspector, ReadOnly] private int _level = 1;
    [ShowInInspector, ReadOnly] private int _currentXP;
    [ShowInInspector, ReadOnly] private int _xpToNextLevel = 100;

    [Tooltip("XP multiplier per level")]
    public float levelXPMultiplier = 1.5f;
    
    public int level => _level;
    public int currentXP => _currentXP;
    public int xpToNextLevel => _xpToNextLevel;
    public float levelProgress => (float)_currentXP / _xpToNextLevel;

    public event Action<int> OnLevelUp;

    public void AddXP(int amount) {
      if (amount <= 0) return;
      
      _currentXP += amount;
      while (_currentXP >= _xpToNextLevel) {
        _currentXP -= _xpToNextLevel;
        _level++;
        _xpToNextLevel = Mathf.RoundToInt(_xpToNextLevel * levelXPMultiplier);
        OnLevelUp?.Invoke(_level);
        Debug.Log($"[Experience] Level up! Now level {_level}");
      }
    }

    public void SetLevel(int level) {
      _level = Mathf.Max(1, level);
      _currentXP = 0;
      _xpToNextLevel = 100;
      for (int i = 1; i < _level; i++) {
        _xpToNextLevel = Mathf.RoundToInt(_xpToNextLevel * levelXPMultiplier);
      }
    }
  }
}
