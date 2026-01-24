using UnityEngine;

namespace Content.Scripts.Game.Camera {
  /// <summary>
  /// Holds the current state of the camera (shared between components)
  /// </summary>
  public class CameraState {
    /// <summary>
    /// Normalized zoom value (0 = closest, 1 = farthest)
    /// </summary>
    public float NormalizedZoom { get; set; } = 0.5f;

    /// <summary>
    /// Target normalized zoom (for smooth interpolation)
    /// </summary>
    public float TargetZoom { get; set; } = 0.5f;

    /// <summary>
    /// Current yaw rotation (Y-axis rotation in degrees)
    /// </summary>
    public float CurrentYaw { get; set; }

    /// <summary>
    /// Target yaw rotation (for smooth interpolation)
    /// </summary>
    public float TargetYaw { get; set; }

    /// <summary>
    /// Current focus point in world space (what the camera orbits around)
    /// </summary>
    public Vector3 FocusPoint { get; set; }

    /// <summary>
    /// Current camera height (calculated from zoom)
    /// </summary>
    public float CurrentHeight { get; set; }

    /// <summary>
    /// Current pitch angle in degrees (calculated from zoom curve)
    /// </summary>
    public float CurrentPitch { get; set; }

    /// <summary>
    /// Whether the camera is currently in follow mode
    /// </summary>
    public bool IsFollowMode { get; set; }
  }
}

