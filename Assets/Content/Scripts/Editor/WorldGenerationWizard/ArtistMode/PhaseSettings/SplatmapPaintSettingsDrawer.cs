#if UNITY_EDITOR
using Content.Scripts.World;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.PhaseSettings {
  /// <summary>
  /// Settings drawer for Splatmap Paint phase.
  /// </summary>
  public class SplatmapPaintSettingsDrawer : IPhaseSettingsDrawer {
    public string PhaseName => "Splatmap Paint";
    public int PhaseIndex => 2;
    public bool IsFoldedOut { get; set; } = true;

    public void Draw(WorldGeneratorConfigSO config, GUIStyle boxStyle) {
      IsFoldedOut = EditorGUILayout.Foldout(IsFoldedOut, $"⚙ {PhaseName} Settings", true);
      if (!IsFoldedOut) return;

      EditorGUILayout.BeginVertical(boxStyle);
      
      var data = config.Data;
      data.paintSplatmap = EditorGUILayout.Toggle("Paint Splatmap", data.paintSplatmap);
      
      EditorGUILayout.Space(4);
      EditorGUILayout.HelpBox("Terrain textures are configured via TerrainPaletteSO and BiomeSO assets.", MessageType.Info);

      EditorGUILayout.EndVertical();
    }
  }
}
#endif
