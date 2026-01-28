#if UNITY_EDITOR
using Content.Scripts.World;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.PhaseSettings {
  /// <summary>
  /// Settings drawer for Scatter phase.
  /// </summary>
  public class ScatterSettingsDrawer : IPhaseSettingsDrawer {
    public string PhaseName => "Scatter";
    public int PhaseIndex => 4;
    public bool IsFoldedOut { get; set; } = true;

    public void Draw(WorldGeneratorConfigSO config, GUIStyle boxStyle) {
      IsFoldedOut = EditorGUILayout.Foldout(IsFoldedOut, $"⚙ {PhaseName} Settings", true);
      if (!IsFoldedOut) return;

      EditorGUILayout.BeginVertical(boxStyle);
      
      var data = config.Data;
      data.createScattersInEditor = EditorGUILayout.Toggle("Create in Editor", data.createScattersInEditor);
      
      EditorGUILayout.Space(4);
      EditorGUILayout.HelpBox("Scatter rules are configured via ScatterRuleSO assets referenced in BiomeSO.", MessageType.Info);

      EditorGUILayout.EndVertical();
    }
  }
}
#endif
