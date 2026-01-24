using Content.Scripts.Game.Camera.Settings;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Camera.Components {
  /// <summary>
  /// Runtime calibration component - attach to any GameObject for live tweaking
  /// Values are synced back to the ScriptableObject in Editor
  /// </summary>
  public class CameraRuntimeCalibrator : MonoBehaviour {
    [Title("Settings Reference")]
    [Required]
    [SerializeField]
    private CameraSettingsSO settings;

    [Title("Zoom Calibration")]
    [MinMaxSlider(1f, 100f, true)]
    [OnValueChanged("ApplyHeightRange")]
    public Vector2 heightRange = new(5f, 50f);

    [Range(0.1f, 10f)]
    [OnValueChanged("ApplyZoomSpeed")]
    public float zoomSpeed = 3f;

    [Range(0.01f, 1f)]
    [OnValueChanged("ApplyZoomSmoothing")]
    public float zoomSmoothing = 0.15f;

    [Title("Pan Calibration")]
    [Range(1f, 100f)]
    [OnValueChanged("ApplyPanSpeed")]
    public float basePanSpeed = 20f;

    [Range(0.01f, 1f)]
    [OnValueChanged("ApplyPanSmoothing")]
    public float panSmoothing = 0.1f;

    [Title("Edge Panning")]
    [OnValueChanged("ApplyEdgePanning")]
    public bool enableEdgePanning = true;

    [ShowIf("enableEdgePanning")]
    [Range(1f, 100f)]
    [OnValueChanged("ApplyEdgePanSpeed")]
    public float edgePanSpeed = 15f;

    [ShowIf("enableEdgePanning")]
    [Range(0.01f, 0.2f)]
    [OnValueChanged("ApplyEdgeThreshold")]
    public float edgeThreshold = 0.05f;

    [Title("Rotation Calibration")]
    [Range(10f, 360f)]
    [OnValueChanged("ApplyRotationSpeed")]
    public float rotationSpeed = 90f;

    [Range(0.01f, 1f)]
    [OnValueChanged("ApplyRotationSmoothing")]
    public float rotationSmoothing = 0.1f;

    [Range(15f, 90f)]
    [OnValueChanged("ApplyDiscreteRotation")]
    public float discreteRotationStep = 45f;

    [Title("Actions")]
    [Button(ButtonSizes.Medium)]
    private void LoadFromSettings() {
      if (settings == null) return;

      heightRange = settings.heightRange;
      zoomSpeed = settings.zoomSpeed;
      zoomSmoothing = settings.zoomSmoothing;
      basePanSpeed = settings.basePanSpeed;
      panSmoothing = settings.panSmoothing;
      enableEdgePanning = settings.enableEdgePanning;
      edgePanSpeed = settings.edgePanSpeed;
      edgeThreshold = settings.edgeThreshold;
      rotationSpeed = settings.rotationSpeed;
      rotationSmoothing = settings.rotationSmoothing;
      discreteRotationStep = settings.discreteRotationStep;
    }

    [Button(ButtonSizes.Medium)]
    private void SaveToSettings() {
      if (settings == null) return;

      settings.heightRange = heightRange;
      settings.zoomSpeed = zoomSpeed;
      settings.zoomSmoothing = zoomSmoothing;
      settings.basePanSpeed = basePanSpeed;
      settings.panSmoothing = panSmoothing;
      settings.enableEdgePanning = enableEdgePanning;
      settings.edgePanSpeed = edgePanSpeed;
      settings.edgeThreshold = edgeThreshold;
      settings.rotationSpeed = rotationSpeed;
      settings.rotationSmoothing = rotationSmoothing;
      settings.discreteRotationStep = discreteRotationStep;

#if UNITY_EDITOR
      UnityEditor.EditorUtility.SetDirty(settings);
#endif
    }

    private void Start() {
      LoadFromSettings();
    }

    // Apply methods for live updates
    private void ApplyHeightRange() { if (settings != null) settings.heightRange = heightRange; }
    private void ApplyZoomSpeed() { if (settings != null) settings.zoomSpeed = zoomSpeed; }
    private void ApplyZoomSmoothing() { if (settings != null) settings.zoomSmoothing = zoomSmoothing; }
    private void ApplyPanSpeed() { if (settings != null) settings.basePanSpeed = basePanSpeed; }
    private void ApplyPanSmoothing() { if (settings != null) settings.panSmoothing = panSmoothing; }
    private void ApplyEdgePanning() { if (settings != null) settings.enableEdgePanning = enableEdgePanning; }
    private void ApplyEdgePanSpeed() { if (settings != null) settings.edgePanSpeed = edgePanSpeed; }
    private void ApplyEdgeThreshold() { if (settings != null) settings.edgeThreshold = edgeThreshold; }
    private void ApplyRotationSpeed() { if (settings != null) settings.rotationSpeed = rotationSpeed; }
    private void ApplyRotationSmoothing() { if (settings != null) settings.rotationSmoothing = rotationSmoothing; }
    private void ApplyDiscreteRotation() { if (settings != null) settings.discreteRotationStep = discreteRotationStep; }
  }
}

