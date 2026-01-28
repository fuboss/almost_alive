#if UNITY_EDITOR
using Content.Scripts.World;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.PhaseSettings {
  /// <summary>
  /// Settings drawer for Vegetation phase.
  /// </summary>
  public class VegetationSettingsDrawer : IPhaseSettingsDrawer {
    public string PhaseName => "Vegetation";
    public int PhaseIndex => 3;
    public bool IsFoldedOut { get; set; } = true;

    public void Draw(WorldGeneratorConfigSO config, GUIStyle boxStyle) {
      IsFoldedOut = EditorGUILayout.Foldout(IsFoldedOut, $"⚙ {PhaseName} Settings", true);
      if (!IsFoldedOut) return;

      EditorGUILayout.BeginVertical(boxStyle);
      
      var data = config.Data;
      data.paintVegetation = EditorGUILayout.Toggle("Paint Vegetation", data.paintVegetation);
      
      EditorGUILayout.Space(4);
      EditorGUILayout.HelpBox("Grass and detail prototypes are configured per BiomeSO asset.", MessageType.Info);

      EditorGUILayout.EndVertical();
    }
  }
}
#endif
