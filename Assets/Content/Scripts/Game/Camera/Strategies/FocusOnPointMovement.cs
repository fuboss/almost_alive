using Content.Scripts.Game.Camera.Settings;
using UnityEngine;

namespace Content.Scripts.Game.Camera.Strategies {
  /// <summary>
  /// Camera movement that animates to a specific world point
  /// Used for "focus on selection" or "jump to location" features
  /// </summary>
  public class FocusOnPointMovement : ICameraMovementStrategy {
    private readonly CameraSettingsSO _settings;
    
    private Vector3 _targetPoint;
    private bool _isActive;
    private float _arrivalThreshold = 0.5f;

    public int priority => 5; // Between free (0) and follow (10)
    public bool isActive => _isActive;

    public FocusOnPointMovement(CameraSettingsSO settings) {
      _settings = settings;
    }

    public Vector3 UpdateFocusPoint(Vector3 currentFocusPoint, float deltaTime) {
      var distance = Vector3.Distance(currentFocusPoint, _targetPoint);
      
      if (distance < _arrivalThreshold) {
        _isActive = false;
        return _targetPoint;
      }

      var t = _settings.followSmoothing * 15f * deltaTime;
      return Vector3.Lerp(currentFocusPoint, _targetPoint, t);
    }

    /// <summary>
    /// Start focusing on a specific world point
    /// </summary>
    public void FocusOn(Vector3 worldPoint) {
      _targetPoint = worldPoint;
      _isActive = true;
    }

    /// <summary>
    /// Cancel the focus movement
    /// </summary>
    public void Cancel() {
      _isActive = false;
    }

    public void OnActivate() { }

    public void OnDeactivate() {
      _isActive = false;
    }
  }
}

