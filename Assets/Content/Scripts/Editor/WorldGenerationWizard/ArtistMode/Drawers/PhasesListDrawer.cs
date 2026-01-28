#if UNITY_EDITOR
using Content.Scripts.World.Generation.Pipeline;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.Drawers {
  /// <summary>
  /// Draws the list of generation phases with toggles and action buttons.
  /// </summary>
  public class PhasesListDrawer : ArtistModeDrawerBase {
    public PhasesListDrawer(ArtistModeState state) : base(state) { }

    public override void Draw() {
      EditorGUILayout.LabelField("Generation Phases", ArtistModeStyles.Header);
      EditorGUILayout.Space(4);

      if (State.Pipeline == null) {
        EditorGUILayout.HelpBox("Pipeline not initialized", MessageType.Warning);
        return;
      }

      for (int i = 0; i < State.Pipeline.Phases.Count; i++) {
        DrawPhaseRow(i);
      }
    }

    private void DrawPhaseRow(int index) {
      var phase = State.Pipeline.Phases[index];
      var state = phase.State;
      var isCurrent = State.Pipeline.CurrentPhaseIndex == index;
      var isTarget = State.TargetPhaseIndex == index;
      var isCompleted = state == PhaseState.Completed;
      var canRun = State.CanRunPhase(index);

      // Background color
      Color bgColor;
      if (isCurrent && state == PhaseState.Running) {
        bgColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
      } else if (isCompleted) {
        bgColor = new Color(0.3f, 0.7f, 0.3f, 0.2f);
      } else if (isTarget) {
        bgColor = new Color(0.9f, 0.7f, 0.2f, 0.15f);
      } else {
        bgColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);
      }

      var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(32));
      EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 30), bgColor);

      GUILayout.Space(4);

      // Toggle (enable/disable this as target phase)
      EditorGUI.BeginChangeCheck();
      var wasEnabled = State.TargetPhaseIndex >= index;
      var isEnabled = GUILayout.Toggle(wasEnabled, "", GUILayout.Width(18));
      if (EditorGUI.EndChangeCheck()) {
        if (isEnabled && !wasEnabled) {
          State.TargetPhaseIndex = index;
        } else if (!isEnabled && wasEnabled) {
          State.TargetPhaseIndex = index - 1;
        }
      }

      // Status icon
      var (icon, iconColor) = GetPhaseIconAndColor(state, isCurrent);
      var oldColor = GUI.color;
      GUI.color = iconColor;
      GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
      GUI.color = oldColor;

      // Phase name
      var nameStyle = isCurrent || isCompleted ? ArtistModeStyles.PhaseNameBold : ArtistModeStyles.PhaseName;
      GUILayout.Label($"{index + 1}. {phase.Name}", nameStyle);

      GUILayout.FlexibleSpace();

      // Run button
      EditorGUI.BeginDisabledGroup(!canRun);
      var runTooltip = isCompleted ? "Regenerate this phase" : "Run to this phase";
      if (GUILayout.Button(new GUIContent("▶", runTooltip), GUILayout.Width(26), GUILayout.Height(22))) {
        State.RunPhase(index);
      }
      EditorGUI.EndDisabledGroup();

      // Rollback button
      EditorGUI.BeginDisabledGroup(!isCompleted);
      if (GUILayout.Button(new GUIContent("↺", "Rollback this phase"), GUILayout.Width(26), GUILayout.Height(22))) {
        State.RollbackPhase(index);
      }
      EditorGUI.EndDisabledGroup();

      GUILayout.Space(4);

      EditorGUILayout.EndHorizontal();
    }

    private (string icon, Color color) GetPhaseIconAndColor(PhaseState state, bool isCurrent) {
      return state switch {
        PhaseState.Pending => ("○", new Color(0.5f, 0.5f, 0.5f)),
        PhaseState.Running => ("●", new Color(0.4f, 0.7f, 1f)),
        PhaseState.Completed => ("✓", new Color(0.4f, 0.9f, 0.4f)),
        PhaseState.Failed => ("✗", new Color(1f, 0.4f, 0.4f)),
        PhaseState.Skipped => ("⊘", new Color(0.8f, 0.8f, 0.4f)),
        _ => ("?", Color.white)
      };
    }
  }
}
#endif
