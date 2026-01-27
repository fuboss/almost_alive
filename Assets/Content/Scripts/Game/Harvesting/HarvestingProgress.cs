using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Harvesting {
  /// <summary>
  /// Tracks harvesting work progress on a harvestable actor.
  /// Added when agent starts harvesting, similar to ChoppingProgress.
  /// </summary>
  public class HarvestingProgress : MonoBehaviour {
    [ShowInInspector, ReadOnly] private float _workProgress;
    [ShowInInspector, ReadOnly] private float _workPerUnit;

    private ActorDescription _actor;
    private GrowthProgress _growthProgress;

    public float workProgress => _workProgress;
    public float workPerUnit => _workPerUnit;
    public float progressToNextUnit => _workPerUnit > 0f ? Mathf.Clamp01(_workProgress / _workPerUnit) : 1f;
    public bool isUnitReady => _workProgress >= _workPerUnit;
    public ActorDescription actor => _actor;
    public GrowthProgress growthProgress => _growthProgress;

    private void Awake() {
      _actor = GetComponent<ActorDescription>();
      _growthProgress = GetComponent<GrowthProgress>();
    }

    private void OnEnable() {
      ActorRegistry<HarvestingProgress>.Register(this);
    }

    private void OnDisable() {
      ActorRegistry<HarvestingProgress>.Unregister(this);
    }

    /// <summary>
    /// Initialize with work required per single harvest unit.
    /// </summary>
    public void Initialize(float workPerUnit) {
      _workPerUnit = workPerUnit;
      _workProgress = 0f;
    }

    /// <summary>
    /// Add work progress. Returns true if one unit is ready to harvest.
    /// </summary>
    public bool AddWork(float amount) {
      if (amount <= 0f) return isUnitReady;
      _workProgress += amount;
      return isUnitReady;
    }

    /// <summary>
    /// Consume one unit of work progress after harvesting.
    /// </summary>
    public void ConsumeUnit() {
      _workProgress = Mathf.Max(0f, _workProgress - _workPerUnit);
    }

    /// <summary>
    /// Reset work progress (e.g., if harvesting was interrupted).
    /// </summary>
    public void Reset() {
      _workProgress = 0f;
    }

    /// <summary>
    /// Gets or adds HarvestingProgress component to target.
    /// </summary>
    public static HarvestingProgress GetOrCreate(GameObject target, float workPerUnit) {
      var progress = target.GetComponent<HarvestingProgress>();
      if (progress == null) {
        progress = target.AddComponent<HarvestingProgress>();
      }
      progress.Initialize(workPerUnit);
      return progress;
    }
  }
}
