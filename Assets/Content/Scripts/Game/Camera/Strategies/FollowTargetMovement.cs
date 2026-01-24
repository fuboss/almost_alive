using Content.Scripts.Game.Camera.Settings;
using Unity.Cinemachine;
using UnityEngine;

namespace Content.Scripts.Game.Camera.Strategies {
  /// <summary>
  /// Camera movement that follows a target group (selected units)
  /// </summary>
  public class FollowTargetMovement : ICameraMovementStrategy {
    private readonly CameraSettingsSO _settings;
    private readonly CinemachineTargetGroup _targetGroup;

    private bool _isActive;

    public int priority => 10; // Higher priority than free movement
    public bool isActive => _isActive && HasValidTargets();

    public FollowTargetMovement(CameraSettingsSO settings, CinemachineTargetGroup targetGroup) {
      _settings = settings;
      _targetGroup = targetGroup;
    }

    public Vector3 UpdateFocusPoint(Vector3 currentFocusPoint, float deltaTime) {
      if (!HasValidTargets()) {
        return currentFocusPoint;
      }

      var targetPosition = _targetGroup.transform.position;
      
      return Vector3.Lerp(
        currentFocusPoint,
        targetPosition,
        _settings.followSmoothing * 10f * deltaTime
      );
    }

    public void SetActive(bool active) {
      if (_isActive == active) return;
      
      if (active) {
        OnActivate();
      } else {
        OnDeactivate();
      }
      
      _isActive = active;
    }

    public void OnActivate() {
      _isActive = true;
    }

    public void OnDeactivate() {
      _isActive = false;
    }

    private bool HasValidTargets() {
      return _targetGroup != null 
             && _targetGroup.Targets != null 
             && _targetGroup.Targets.Count > 0;
    }
  }
}
