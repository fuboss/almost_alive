#if UNITY_EDITOR
using Content.Scripts.World.Generation.Pipeline;
using UnityEditor;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.Drawers {
  /// <summary>
  /// Draws status text at the bottom of the window.
  /// </summary>
  public class StatusDrawer : ArtistModeDrawerBase {
    public StatusDrawer(ArtistModeState state) : base(state) { }

    public override void Draw() {
      var status = GetStatusText();
      EditorGUILayout.LabelField(status, EditorStyles.centeredGreyMiniLabel);
    }

    private string GetStatusText() {
      if (State.Pipeline == null) return "Pipeline not ready";
      if (!State.Pipeline.IsRunning) return "Ready to generate";
      if (State.Pipeline.IsCompleted) return "✓ Generation complete";
      if (State.Pipeline.IsPaused) {
        var current = State.Pipeline.CurrentPhase?.Name ?? "—";
        return $"Paused after: {current}";
      }
      return $"Running phase {State.Pipeline.CurrentPhaseIndex + 1}...";
    }
  }
}
#endif
