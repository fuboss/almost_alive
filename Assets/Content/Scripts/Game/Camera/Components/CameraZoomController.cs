using Content.Scripts.Game.Camera.Input;
using Content.Scripts.Game.Camera.Settings;
using UnityEngine;

namespace Content.Scripts.Game.Camera.Components {
  /// <summary>
  /// Controls camera zoom with dynamic pitch adjustment based on curve
  /// </summary>
  public class CameraZoomController {
    private readonly CameraSettingsSO _settings;
    private readonly CameraInputHandler _input;
    private readonly CameraState _state;

    public CameraZoomController(CameraSettingsSO settings, CameraInputHandler input, CameraState state) {
      _settings = settings;
      _input = input;
      _state = state;
    }

    public void Update(float deltaTime) {
      ProcessZoomInput();
      SmoothZoom(deltaTime);
      UpdateHeightAndPitch();
    }

    private void ProcessZoomInput() {
      var zoomInput = _input.ZoomInput;
      if (Mathf.Abs(zoomInput) < 0.01f) return;

      // Normalize scroll input and apply speed
      var zoomDelta = -Mathf.Sign(zoomInput) * _settings.zoomSpeed * 0.01f;
      _state.TargetZoom = Mathf.Clamp01(_state.TargetZoom + zoomDelta);
    }

    private void SmoothZoom(float deltaTime) {
      _state.NormalizedZoom = Mathf.Lerp(
        _state.NormalizedZoom,
        _state.TargetZoom,
        _settings.zoomSmoothing * 10f * deltaTime
      );
    }

    private void UpdateHeightAndPitch() {
      _state.CurrentHeight = _settings.GetHeightForZoom(_state.NormalizedZoom);
      _state.CurrentPitch = _settings.GetPitchForZoom(_state.NormalizedZoom);
    }

    /// <summary>
    /// Sets zoom level directly (for external control)
    /// </summary>
    public void SetZoom(float normalizedZoom, bool immediate = false) {
      _state.TargetZoom = Mathf.Clamp01(normalizedZoom);
      if (immediate) {
        _state.NormalizedZoom = _state.TargetZoom;
        UpdateHeightAndPitch();
      }
    }
  }
}

