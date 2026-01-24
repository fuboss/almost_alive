using Content.Scripts.Game.Camera.Input;
using Content.Scripts.Game.Camera.Settings;
using UnityEngine;

namespace Content.Scripts.Game.Camera.Components {
  /// <summary>
  /// Controls camera rotation (Q/E discrete + middle mouse drag)
  /// </summary>
  public class CameraRotationController {
    private readonly CameraSettingsSO _settings;
    private readonly CameraInputHandler _input;
    private readonly CameraState _state;

    public CameraRotationController(CameraSettingsSO settings, CameraInputHandler input, CameraState state) {
      _settings = settings;
      _input = input;
      _state = state;
    }

    public void Update(float deltaTime) {
      ProcessDiscreteRotation();
      ProcessMouseRotation(deltaTime);
      SmoothRotation(deltaTime);
    }

    private void ProcessDiscreteRotation() {
      var discrete = _input.DiscreteRotation;
      if (Mathf.Abs(discrete) > 0.5f) {
        _state.TargetYaw += discrete * _settings.discreteRotationStep;
        _input.ConsumeDiscreteRotation();
      }
    }

    private void ProcessMouseRotation(float deltaTime) {
      if (!_input.IsMiddleMouseHeld) return;

      var mouseDelta = _input.MouseDelta;
      if (Mathf.Abs(mouseDelta.x) < 0.1f) return;

      _state.TargetYaw += mouseDelta.x * _settings.rotationSpeed * deltaTime * 0.1f;
    }

    private void SmoothRotation(float deltaTime) {
      // Normalize yaw to 0-360 range
      _state.TargetYaw = NormalizeAngle(_state.TargetYaw);
      
      // Use smooth damp for rotation to handle angle wrapping
      _state.CurrentYaw = Mathf.LerpAngle(
        _state.CurrentYaw,
        _state.TargetYaw,
        _settings.rotationSmoothing * 10f * deltaTime
      );
      
      _state.CurrentYaw = NormalizeAngle(_state.CurrentYaw);
    }

    /// <summary>
    /// Sets rotation directly (for external control)
    /// </summary>
    public void SetYaw(float yaw, bool immediate = false) {
      _state.TargetYaw = NormalizeAngle(yaw);
      if (immediate) {
        _state.CurrentYaw = _state.TargetYaw;
      }
    }

    private static float NormalizeAngle(float angle) {
      while (angle < 0f) angle += 360f;
      while (angle >= 360f) angle -= 360f;
      return angle;
    }
  }
}
