using Content.Scripts.Building.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Runtime module instance placed in a slot.
  /// </summary>
  public class Module : MonoBehaviour {
    [ShowInInspector, ReadOnly]
    private ModuleDefinitionSO _definition;
    
    [ShowInInspector, ReadOnly]
    private object _owner;  // IGoapAgent
    
    [ShowInInspector, ReadOnly]
    private float _hp;
    
    [ShowInInspector, ReadOnly]
    private float _maxHp;

    public ModuleDefinitionSO definition => _definition;
    public object owner => _owner;
    public float hp => _hp;
    public float maxHp => _maxHp;
    public bool isDamaged => _hp < _maxHp;
    public float hpPercent => _maxHp > 0 ? _hp / _maxHp : 0f;

    /// <summary>
    /// Initialize module after instantiation.
    /// </summary>
    public void Initialize(ModuleDefinitionSO definition, float maxHp = 100f) {
      _definition = definition;
      _maxHp = maxHp;
      _hp = maxHp;
      _owner = null;
    }

    /// <summary>
    /// Check if agent can use this module.
    /// </summary>
    public bool CanUse(object agent) {
      // No owner = anyone can use
      if (_owner == null) return true;
      // Owner match
      return _owner == agent;
    }

    /// <summary>
    /// Set owner of this module.
    /// </summary>
    public void SetOwner(object agent) {
      _owner = agent;
    }

    /// <summary>
    /// Clear ownership.
    /// </summary>
    public void ClearOwner() {
      _owner = null;
    }

    /// <summary>
    /// Apply damage to module.
    /// </summary>
    public void TakeDamage(float damage) {
      _hp = Mathf.Max(0, _hp - damage);
    }

    /// <summary>
    /// Repair module.
    /// </summary>
    public void Repair(float amount) {
      _hp = Mathf.Min(_maxHp, _hp + amount);
    }

    /// <summary>
    /// Fully repair module.
    /// </summary>
    public void FullRepair() {
      _hp = _maxHp;
    }
  }
}
