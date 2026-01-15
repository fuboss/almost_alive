using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game {
  /// <summary>
  /// Priority of actor for work assignment (storage filling, etc).
  /// 0 = disabled, 1-9 = priority levels (9 = critical).
  /// </summary>
  public class ActorPriority : MonoBehaviour {
    [Range(0, 9)]
    [Tooltip("0 = disabled, 1 = lowest, 9 = critical")]
    [SerializeField] private int _priority = 5;

    public int priority {
      get => _priority;
      set => _priority = Mathf.Clamp(value, 0, 9);
    }

    public bool isEnabled => _priority > 0;

    private void OnEnable() {
      ActorRegistry<ActorPriority>.Register(this);
    }

    private void OnDisable() {
      ActorRegistry<ActorPriority>.Unregister(this);
    }

    public void SetPriority(int value) {
      priority = value;
    }

    public void Disable() {
      priority = 0;
    }

    public void SetCritical() {
      priority = 9;
    }
  }
}
