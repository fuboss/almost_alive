using UnityEngine;

namespace Content.Scripts.Ui.Layers.WorldSpaceUI {
  public static class UIHelper {
    public static Vector2 WorldToCanvasPosition(RectTransform rect, Camera camera, Vector3 position) {
      if (camera == null) {
        return Vector2.zero;
      }

      if (rect == null) {
        return Vector2.zero;
      }

      Vector2 canvasPosition = default;
      canvasPosition = camera.WorldToViewportPoint(position);
      canvasPosition.x *= rect.sizeDelta.x;
      canvasPosition.y *= rect.sizeDelta.y;

      canvasPosition.x -= rect.sizeDelta.x * rect.pivot.x;
      canvasPosition.y -= rect.sizeDelta.y * rect.pivot.y;

      return canvasPosition;
    }

    public static bool IsWorldPosOnScreen(Camera camera, Vector3 pos) {
      var point = camera.WorldToViewportPoint(pos);
      return point.x is >= 0 and <= 1 && point.y is >= 0 and <= 1 && point.z > 0;
    }
  }
}