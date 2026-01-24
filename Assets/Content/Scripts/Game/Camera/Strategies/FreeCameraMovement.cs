using Content.Scripts.Game.Camera.Input;
using Content.Scripts.Game.Camera.Settings;
using UnityEngine;

namespace Content.Scripts.Game.Camera.Strategies {
  /// <summary>
  /// Free RTS camera movement - WASD + Edge panning
  /// </summary>
  public class FreeCameraMovement : ICameraMovementStrategy {
    private readonly CameraSettingsSO _settings;
    private readonly CameraInputHandler _input;
    private readonly CameraState _state;

    private Vector3 _velocity;

    public int priority => 0; // Lowest priority - default mode
    public bool isActive => true; // Always active as fallback

    public FreeCameraMovement(CameraSettingsSO settings, CameraInputHandler input, CameraState state) {
      _settings = settings;
      _input = input;
      _state = state;
    }

    public Vector3 UpdateFocusPoint(Vector3 currentFocusPoint, float deltaTime) {
      var moveInput = _input.MoveInput;
      
      if (_settings.enableEdgePanning) {
        moveInput += GetEdgePanInput();
      }

      if (moveInput.sqrMagnitude < 0.001f) {
        _velocity = Vector3.Lerp(_velocity, Vector3.zero, _settings.panSmoothing * 10f * deltaTime);
        return currentFocusPoint + _velocity * deltaTime;
      }

      var yawRotation = Quaternion.Euler(0f, _state.CurrentYaw, 0f);
      var moveDirection = yawRotation * new Vector3(moveInput.x, 0f, moveInput.y);

      var speedMultiplier = _settings.GetPanSpeedMultiplier(_state.NormalizedZoom);
      var targetSpeed = _settings.basePanSpeed * speedMultiplier;
      
      var targetVelocity = moveDirection * targetSpeed;
      _velocity = Vector3.Lerp(_velocity, targetVelocity, _settings.panSmoothing * 10f * deltaTime);

      var newPosition = currentFocusPoint + _velocity * deltaTime;

      if (_settings.enableBoundaries) {
        newPosition = ClampToBounds(newPosition);
      }

      return newPosition;
    }

    private Vector2 GetEdgePanInput() {
      var mousePos = _input.MouseScreenPosition;
      var screenSize = new Vector2(Screen.width, Screen.height);
      var normalizedPos = mousePos / screenSize;

      var edgeInput = Vector2.zero;
      var threshold = _settings.edgeThreshold;
      var speedRatio = _settings.edgePanSpeed / _settings.basePanSpeed;

      // Left edge
      if (normalizedPos.x < threshold) {
        edgeInput.x = -Mathf.InverseLerp(threshold, 0f, normalizedPos.x) * speedRatio;
      }
      // Right edge
      else if (normalizedPos.x > 1f - threshold) {
        edgeInput.x = Mathf.InverseLerp(1f - threshold, 1f, normalizedPos.x) * speedRatio;
      }

      // Bottom edge
      if (normalizedPos.y < threshold) {
        edgeInput.y = -Mathf.InverseLerp(threshold, 0f, normalizedPos.y) * speedRatio;
      }
      // Top edge
      else if (normalizedPos.y > 1f - threshold) {
        edgeInput.y = Mathf.InverseLerp(1f - threshold, 1f, normalizedPos.y) * speedRatio;
      }

      return edgeInput;
    }

    private Vector3 ClampToBounds(Vector3 position) {
      var bounds = _settings.worldBounds;
      return new Vector3(
        Mathf.Clamp(position.x, bounds.min.x, bounds.max.x),
        position.y,
        Mathf.Clamp(position.z, bounds.min.z, bounds.max.z)
      );
    }

    public void OnActivate() {
      _velocity = Vector3.zero;
    }

    public void OnDeactivate() { }
  }
}

