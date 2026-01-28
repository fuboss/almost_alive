#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode {
  /// <summary>
  /// Shared GUI styles for Artist Mode window.
  /// </summary>
  public static class ArtistModeStyles {
    public static GUIStyle Header { get; private set; }
    public static GUIStyle PhaseName { get; private set; }
    public static GUIStyle PhaseNameBold { get; private set; }
    public static GUIStyle Status { get; private set; }
    public static GUIStyle Box { get; private set; }

    private static bool _initialized;

    public static void Initialize() {
      if (_initialized) return;

      Header = new GUIStyle(EditorStyles.boldLabel) {
        fontSize = 14,
        alignment = TextAnchor.MiddleLeft
      };

      PhaseName = new GUIStyle(EditorStyles.label) {
        alignment = TextAnchor.MiddleLeft
      };

      PhaseNameBold = new GUIStyle(EditorStyles.boldLabel) {
        alignment = TextAnchor.MiddleLeft
      };

      Status = new GUIStyle(EditorStyles.miniLabel) {
        alignment = TextAnchor.MiddleRight,
        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
      };

      Box = new GUIStyle("box") {
        padding = new RectOffset(8, 8, 6, 6),
        margin = new RectOffset(0, 0, 2, 2)
      };

      _initialized = true;
    }

    public static void Reset() {
      _initialized = false;
    }
  }
}
#endif
