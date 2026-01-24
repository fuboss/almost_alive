using UnityEngine;

namespace Content.Scripts.Game.Camera.Components {
  /// <summary>
  /// Helper component for visualizing and calibrating camera settings in editor
  /// Attach to CinemachineCamera GameObject
  /// </summary>
  public class CameraDebugVisualizer : MonoBehaviour {
    [Header("References")]
    public CameraState state;
    
    [Header("Debug Display")]
    public bool showGizmos = true;
    public bool showOnScreenInfo = true;
    public Color focusPointColor = Color.yellow;
    public Color boundsColor = Color.green;

    private void OnDrawGizmos() {
      if (!showGizmos || state == null) return;

      // Draw focus point
      Gizmos.color = focusPointColor;
      Gizmos.DrawWireSphere(state.FocusPoint, 1f);
      
      // Draw camera direction
      var cameraPos = CalculateCameraPosition();
      Gizmos.color = Color.cyan;
      Gizmos.DrawLine(cameraPos, state.FocusPoint);
      Gizmos.DrawWireSphere(cameraPos, 0.5f);
    }

    private Vector3 CalculateCameraPosition() {
      var rotation = Quaternion.Euler(state.CurrentPitch, state.CurrentYaw, 0f);
      var offset = rotation * Vector3.back * state.CurrentHeight;
      return state.FocusPoint + offset;
    }

#if UNITY_EDITOR
    private void OnGUI() {
      if (!showOnScreenInfo || state == null) return;

      var style = new GUIStyle(GUI.skin.box) {
        fontSize = 14,
        alignment = TextAnchor.UpperLeft
      };

      var content = $"Camera Debug:\n" +
                    $"Zoom: {state.NormalizedZoom:F2} (target: {state.TargetZoom:F2})\n" +
                    $"Height: {state.CurrentHeight:F1}m\n" +
                    $"Pitch: {state.CurrentPitch:F1}°\n" +
                    $"Yaw: {state.CurrentYaw:F1}°\n" +
                    $"Focus: {state.FocusPoint.ToString("F1")}\n" +
                    $"Mode: {(state.IsFollowMode ? "Follow" : "Free")}";

      GUI.Box(new Rect(10, 10, 250, 140), content, style);
    }
#endif
  }
}

