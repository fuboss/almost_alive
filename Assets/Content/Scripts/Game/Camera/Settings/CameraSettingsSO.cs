using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Camera.Settings {
  [CreateAssetMenu(fileName = "CameraSettings", menuName = "Game/Camera/Settings")]
  public class CameraSettingsSO : SerializedScriptableObject {
    [Title("Zoom Settings")]
    [MinMaxSlider(0.5f, 100f, true)]
    public Vector2 heightRange = new(1.5f, 50f);
    
    [Tooltip("X = zoom (0-1), Y = pitch angle in degrees. Low zoom = look forward, High zoom = look down")]
    public AnimationCurve zoomToPitchCurve = AnimationCurve.EaseInOut(0f, 15f, 1f, 75f);
    
    [Range(0.1f, 10f)]
    public float zoomSpeed = 3f;
    
    [Range(0.01f, 1f)]
    public float zoomSmoothing = 0.15f;

    [Title("Pan Settings (WASD)")]
    [Range(1f, 100f)]
    public float basePanSpeed = 20f;
    
    [Tooltip("X = zoom (0-1), Y = speed multiplier. Far = faster panning")]
    public AnimationCurve panSpeedByZoomCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 2f);
    
    [Range(0.01f, 1f)]
    public float panSmoothing = 0.1f;

    [Title("Edge Panning")]
    public bool enableEdgePanning = true;
    
    [ShowIf("enableEdgePanning")]
    [Range(1f, 100f)]
    public float edgePanSpeed = 15f;
    
    [ShowIf("enableEdgePanning")]
    [Range(0.01f, 0.2f)]
    [Tooltip("Screen edge threshold (0.05 = 5% from edge)")]
    public float edgeThreshold = 0.05f;

    [Title("Rotation Settings")]
    [Range(10f, 360f)]
    public float rotationSpeed = 90f;
    
    [Range(0.01f, 1f)]
    public float rotationSmoothing = 0.1f;
    
    [Tooltip("Discrete rotation step for Q/E keys (degrees)")]
    [Range(15f, 90f)]
    public float discreteRotationStep = 45f;

    [Title("Boundaries")]
    public bool enableBoundaries = true;
    
    [ShowIf("enableBoundaries")]
    public Bounds worldBounds = new(Vector3.zero, new Vector3(100f, 50f, 100f));

    [Title("Follow Mode")]
    [Range(0.01f, 1f)]
    public float followSmoothing = 0.2f;
    
    [Tooltip("Extra height offset when following targets")]
    public float followHeightOffset = 5f;

    [Title("Debug")]
    public bool showDebugGizmos = false;

    /// <summary>
    /// Gets the target height for a normalized zoom value (0-1)
    /// </summary>
    public float GetHeightForZoom(float normalizedZoom) {
      return Mathf.Lerp(heightRange.x, heightRange.y, normalizedZoom);
    }

    /// <summary>
    /// Gets the pitch angle (X rotation) for a normalized zoom value (0-1)
    /// </summary>
    public float GetPitchForZoom(float normalizedZoom) {
      return zoomToPitchCurve.Evaluate(normalizedZoom);
    }

    /// <summary>
    /// Gets the pan speed multiplier for a normalized zoom value (0-1)
    /// </summary>
    public float GetPanSpeedMultiplier(float normalizedZoom) {
      return panSpeedByZoomCurve.Evaluate(normalizedZoom);
    }
  }
}

