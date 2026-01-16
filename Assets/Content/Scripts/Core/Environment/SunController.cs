using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Core.Environment {
  /// <summary>
  /// Controls Directional Light based on WorldEnvironment.
  /// Attach to your sun light.
  /// </summary>
  [RequireComponent(typeof(Light))]
  public class SunController : MonoBehaviour {
    [SerializeField] private bool _autoRotate = true;
    [SerializeField] private Vector3 _rotationAxis = Vector3.right;
    [SerializeField] private float _additionalSlope = 50;

    [FoldoutGroup("Intensity")] [SerializeField]
    private AnimationCurve _intensityCurve = CreateDefaultIntensityCurve();

    [FoldoutGroup("Intensity")] [SerializeField]
    private float _maxIntensity = 1.2f;

    [FoldoutGroup("Color")] [SerializeField]
    private Gradient _colorGradient = CreateDefaultGradient();

    [FoldoutGroup("Ambient")] [SerializeField]
    private bool _controlAmbient = true;

    [FoldoutGroup("Ambient")] [SerializeField]
    private Gradient _ambientGradient = CreateDefaultAmbientGradient();

    [FoldoutGroup("Ambient")] [SerializeField]
    private float _ambientIntensity = 0.5f;

    private Light _light;

    private void Awake() {
      _light = GetComponent<Light>();
    }

    private void LateUpdate() {
      var env = WorldEnvironment.instance;
      if (env == null) return;

      var t = env.dayCycle.normalizedTime;

      // Rotation
      if (_autoRotate) {
        var angle = env.sunAngle;
        transform.rotation = Quaternion.AngleAxis(angle, _rotationAxis);
        var angles = transform.rotation.eulerAngles;
        angles.y = _additionalSlope;
        transform.rotation = Quaternion.Euler(angles);
      }

      // Intensity & Color
      _light.intensity = _intensityCurve.Evaluate(t) * _maxIntensity;
      _light.color = _colorGradient.Evaluate(t);

      // Ambient
      if (_controlAmbient) {
        RenderSettings.ambientLight = _ambientGradient.Evaluate(t) * _ambientIntensity;
      }
    }

    private static AnimationCurve CreateDefaultIntensityCurve() {
      var c = new AnimationCurve();
      c.AddKey(0f, 0f); // midnight
      c.AddKey(0.2f, 0f); // pre-dawn
      c.AddKey(0.3f, 0.8f); // sunrise
      c.AddKey(0.5f, 1f); // noon
      c.AddKey(0.75f, 0.8f); // sunset
      c.AddKey(0.85f, 0f); // night
      c.AddKey(1f, 0f); // midnight
      return c;
    }

    private static Gradient CreateDefaultGradient() {
      var g = new Gradient();
      g.SetKeys(
        new[] {
          new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f), // night - bluish
          new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.25f), // dawn - orange
          new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0.35f), // morning
          new GradientColorKey(Color.white, 0.5f), // noon
          new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0.7f), // afternoon
          new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.8f), // sunset
          new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0.9f), // night
        },
        new[] {
          new GradientAlphaKey(1f, 0f),
          new GradientAlphaKey(1f, 1f)
        }
      );
      return g;
    }

    private static Gradient CreateDefaultAmbientGradient() {
      var g = new Gradient();
      g.SetKeys(
        new[] {
          new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0f), // night
          new GradientColorKey(new Color(0.3f, 0.25f, 0.2f), 0.25f), // dawn
          new GradientColorKey(new Color(0.5f, 0.5f, 0.6f), 0.4f), // day
          new GradientColorKey(new Color(0.5f, 0.5f, 0.6f), 0.7f), // day
          new GradientColorKey(new Color(0.3f, 0.2f, 0.15f), 0.8f), // dusk
          new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.9f), // night
        },
        new[] {
          new GradientAlphaKey(1f, 0f),
          new GradientAlphaKey(1f, 1f)
        }
      );
      return g;
    }
  }
}