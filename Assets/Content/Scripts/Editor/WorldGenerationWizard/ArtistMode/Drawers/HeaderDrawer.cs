#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.Drawers {
  /// <summary>
  /// Draws the header section with title and debug viz toggle.
  /// </summary>
  public class HeaderDrawer : ArtistModeDrawerBase {
    public HeaderDrawer(ArtistModeState state) : base(state) { }

    public override void Draw() {
      EditorGUILayout.BeginHorizontal();
      GUILayout.Label("🎨 Artist Mode", ArtistModeStyles.Header);
      GUILayout.FlexibleSpace();

      EditorGUILayout.EndHorizontal();

      EditorGUILayout.LabelField("Iterate on each generation phase", EditorStyles.miniLabel);
    }
  }
}
#endif
