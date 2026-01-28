#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.Drawers {
  /// <summary>
  /// Draws seed field with randomize and reset buttons.
  /// </summary>
  public class SeedDrawer : ArtistModeDrawerBase {
    public SeedDrawer(ArtistModeState state) : base(state) { }

    public override void Draw() {
      EditorGUILayout.BeginHorizontal();

      EditorGUILayout.LabelField("Seed", GUILayout.Width(50));

      EditorGUI.BeginChangeCheck();
      State.Seed = EditorGUILayout.IntField(State.Seed);
      if (EditorGUI.EndChangeCheck()) State.ApplySeedToConfig();

      if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh", "Randomize"), GUILayout.Width(28))) {
        State.Seed = Random.Range(1, int.MaxValue);
        State.ApplySeedToConfig();
      }

      if (GUILayout.Button("0", GUILayout.Width(22))) {
        State.Seed = 0;
        State.ApplySeedToConfig();
      }

      EditorGUILayout.EndHorizontal();
    }
  }
}
#endif
