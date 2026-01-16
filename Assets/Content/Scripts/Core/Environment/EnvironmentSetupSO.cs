using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Core.Environment {
  [CreateAssetMenu(menuName = "Environment/Setup", fileName = "EnvironmentSetup")]
  public class EnvironmentSetupSO : ScriptableObject {
    [BoxGroup("Day Cycle")]
    [Tooltip("Length of one day in simulation seconds. 1440 = 24 real-min at 1x")]
    [SerializeField] private float _dayLengthSeconds = 1440f;

    [BoxGroup("Day Cycle")]
    [Tooltip("Normalized time to start simulation (0 = midnight, 0.25 = 6:00, 0.5 = noon)")]
    [Range(0f, 1f)]
    [SerializeField] private float _startTimeNormalized = 0.3f; // ~07:00

    [FoldoutGroup("Phase Thresholds")]
    [Range(0f, 1f)] [SerializeField] private float _dawnStart = 0.2f;
    [FoldoutGroup("Phase Thresholds")]
    [Range(0f, 1f)] [SerializeField] private float _dayStart = 0.3f;
    [FoldoutGroup("Phase Thresholds")]
    [Range(0f, 1f)] [SerializeField] private float _duskStart = 0.75f;
    [FoldoutGroup("Phase Thresholds")]
    [Range(0f, 1f)] [SerializeField] private float _nightStart = 0.85f;

    [FoldoutGroup("AI Modifiers")]
    [Tooltip("Sleepiness by time of day (0 = midnight, 0.5 = noon, 1 = midnight)")]
    [SerializeField] private AnimationCurve _sleepinessCurve = CreateDefaultSleepinessCurve();

    [FoldoutGroup("AI Modifiers")]
    [Tooltip("Vision range multiplier (1 = full, <1 = reduced at night)")]
    [SerializeField] private AnimationCurve _visionCurve = CreateDefaultVisionCurve();

    // Public accessors
    public float dayLengthSeconds => _dayLengthSeconds;
    public float startTimeNormalized => _startTimeNormalized;
    public float startTimeAsSimSeconds => _startTimeNormalized * _dayLengthSeconds;

    public float dawnStart => _dawnStart;
    public float dayStart => _dayStart;
    public float duskStart => _duskStart;
    public float nightStart => _nightStart;

    public float EvaluateSleepiness(float normalizedTime) => _sleepinessCurve.Evaluate(normalizedTime);
    public float EvaluateVision(float normalizedTime) => _visionCurve.Evaluate(normalizedTime);

    [ShowInInspector, ReadOnly, BoxGroup("Preview")]
    public string startTimeFormatted => $"{Mathf.FloorToInt(_startTimeNormalized * 24):00}:{Mathf.FloorToInt((_startTimeNormalized * 24 % 1) * 60):00}";

    private static AnimationCurve CreateDefaultSleepinessCurve() {
      var c = new AnimationCurve();
      c.AddKey(0f, 0.9f);
      c.AddKey(0.25f, 0.3f);
      c.AddKey(0.35f, 0f);
      c.AddKey(0.6f, 0f);
      c.AddKey(0.75f, 0.2f);
      c.AddKey(0.9f, 0.7f);
      c.AddKey(1f, 0.9f);
      return c;
    }

    private static AnimationCurve CreateDefaultVisionCurve() {
      var c = new AnimationCurve();
      c.AddKey(0f, 0.4f);
      c.AddKey(0.2f, 0.5f);
      c.AddKey(0.3f, 1f);
      c.AddKey(0.75f, 1f);
      c.AddKey(0.85f, 0.5f);
      c.AddKey(1f, 0.4f);
      return c;
    }
  }
}
