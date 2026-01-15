using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Decay {
  /// <summary>
  /// Marks actor as "loose in world" and tracks decay time.
  /// Dynamically added when item is dropped, removed when picked up.
  /// </summary>
  public class DecayableActor : MonoBehaviour {
    [SerializeField] private float _maxDecayTime = 300f;
    [ShowInInspector, ReadOnly] private float _currentDecay;
    [ShowInInspector, ReadOnly] private float _droppedTime;

    public float maxDecayTime {
      get => _maxDecayTime;
      set => _maxDecayTime = Mathf.Max(1f, value);
    }

    public float currentDecay => _currentDecay;
    public float progress => _maxDecayTime > 0 ? _currentDecay / _maxDecayTime : 1f;
    public bool isExpired => _currentDecay >= _maxDecayTime;
    public float remainingTime => Mathf.Max(0f, _maxDecayTime - _currentDecay);
    public float droppedTime => _droppedTime;

    private void OnEnable() {
      _droppedTime = Time.time;
      _currentDecay = 0f;
      ActorRegistry<DecayableActor>.Register(this);
    }

    private void OnDisable() {
      ActorRegistry<DecayableActor>.Unregister(this);
    }

    public void TickDecay(float simDeltaTime) {
      if (isExpired) return;
      _currentDecay += simDeltaTime;
    }

    public void ResetDecay() {
      _currentDecay = 0f;
    }

    /// <summary>
    /// Attach DecayableActor to GameObject (when dropping item).
    /// </summary>
    public static DecayableActor AttachTo(GameObject go, float maxTime = 300f) {
      if (go == null) return null;
      
      var existing = go.GetComponent<DecayableActor>();
      if (existing != null) {
        existing.ResetDecay();
        existing.maxDecayTime = maxTime;
        return existing;
      }

      var decay = go.AddComponent<DecayableActor>();
      decay._maxDecayTime = maxTime;
      return decay;
    }

    /// <summary>
    /// Remove DecayableActor from GameObject (when picking up item).
    /// </summary>
    public static void RemoveFrom(GameObject go) {
      if (go == null) return;
      var decay = go.GetComponent<DecayableActor>();
      if (decay != null) {
        Destroy(decay);
      }
    }

    /// <summary>
    /// Check if GameObject is currently decaying (loose in world).
    /// </summary>
    public static bool IsDecaying(GameObject go) {
      return go != null && go.GetComponent<DecayableActor>() != null;
    }
  }
}
