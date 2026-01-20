using Content.Scripts.Game.Craft;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Trees {
  /// <summary>
  /// Tracks tree chopping progress. Added to tree when chopping starts.
  /// Implements IProgressProvider for UI progress bar display.
  /// </summary>
  public class ChoppingProgress : MonoBehaviour, IProgressProvider {
    [ShowInInspector, ReadOnly] private float _workProgress;
    [ShowInInspector, ReadOnly] private float _workRequired;

    private ActorDescription _actor;

    public float workProgress => _workProgress;
    public float workRequired => _workRequired;
    public float progress => _workRequired > 0f ? Mathf.Clamp01(_workProgress / _workRequired) : 1f;
    public bool isComplete => _workProgress >= _workRequired;
    public ActorDescription actor => _actor;

    private void Awake() {
      _actor = GetComponent<ActorDescription>();
    }

    private void OnEnable() {
      ActorRegistry<ChoppingProgress>.Register(this);
    }

    private void OnDisable() {
      ActorRegistry<ChoppingProgress>.Unregister(this);
    }

    /// <summary>Initialize chopping with required work amount.</summary>
    public void Initialize(float requiredWork) {
      _workRequired = requiredWork;
      _workProgress = 0f;
      Debug.Log($"[ChoppingProgress] Initialized on {gameObject.name}, work required: {requiredWork}");
    }

    /// <summary>Add work progress. Returns true if chopping is complete.</summary>
    public bool AddWork(float amount) {
      if (amount <= 0f) return isComplete;
      _workProgress = Mathf.Min(_workProgress + amount, _workRequired);

      TryShakeTree();

      return isComplete;
    }

    private void TryShakeTree() {
      const float interval = 10;
      var scaled = (int)(_workProgress * 10);
      if (scaled >= interval && scaled % interval < 1) {
        //shake the tree
        actor.transform.DOKill();
        actor.transform.DOPunchRotation(Random.onUnitSphere, 0.2f, 20);
      }
    }

    /// <summary>Reset progress (e.g., if chopping was interrupted).</summary>
    public void Reset() {
      _workProgress = 0f;
    }

    /// <summary>
    /// Gets or adds ChoppingProgress component to target.
    /// </summary>
    public static ChoppingProgress GetOrCreate(GameObject target, float workRequired) {
      var progress = target.GetComponent<ChoppingProgress>();
      if (progress == null) {
        progress = target.AddComponent<ChoppingProgress>();
        progress.Initialize(workRequired);
      }

      return progress;
    }
  }
}