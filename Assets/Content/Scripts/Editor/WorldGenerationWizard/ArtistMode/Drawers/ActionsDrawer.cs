#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.Drawers {
  /// <summary>
  /// Draws action buttons: Run to Target, Reset, Clear.
  /// </summary>
  public class ActionsDrawer : ArtistModeDrawerBase {
    public ActionsDrawer(ArtistModeState state) : base(state) { }

    public override void Draw() {
      EditorGUILayout.BeginVertical(ArtistModeStyles.Box);

      EditorGUILayout.BeginHorizontal();

      // Run to target
      EditorGUI.BeginDisabledGroup(!State.CanRunToTarget());
      var targetName = State.TargetPhaseIndex >= 0 && State.Pipeline?.Phases != null &&
                       State.TargetPhaseIndex < State.Pipeline.Phases.Count
        ? State.Pipeline.Phases[State.TargetPhaseIndex].Name
        : "—";
      GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
      if (GUILayout.Button($"▶ Run to: {targetName}", GUILayout.Height(28))) {
        State.RunToTarget();
      }
      GUI.backgroundColor = Color.white;
      EditorGUI.EndDisabledGroup();

      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space(4);

      EditorGUILayout.BeginHorizontal();

      // Reset
      EditorGUI.BeginDisabledGroup(!State.CanReset());
      GUI.backgroundColor = new Color(1f, 0.7f, 0.4f);
      if (GUILayout.Button("↺ Reset", GUILayout.Height(24))) {
        State.Reset();
      }
      GUI.backgroundColor = Color.white;
      EditorGUI.EndDisabledGroup();

      // Clear World
      GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
      if (GUILayout.Button("🗑 Clear", GUILayout.Height(24))) {
        if (EditorUtility.DisplayDialog("Clear World",
            "Reset terrain and remove all generated objects?", "Clear", "Cancel")) {
          State.Clear();
        }
      }
      GUI.backgroundColor = Color.white;

      EditorGUILayout.EndHorizontal();

      EditorGUILayout.EndVertical();
    }
  }
}
#endif
