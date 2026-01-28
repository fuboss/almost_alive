#if UNITY_EDITOR
using Content.Scripts.World;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.Drawers {
  /// <summary>
  /// Draws config and terrain fields section.
  /// </summary>
  public class ConfigDrawer : ArtistModeDrawerBase {
    public ConfigDrawer(ArtistModeState state) : base(state) { }

    public override void Draw() {
      EditorGUILayout.BeginVertical(ArtistModeStyles.Box);

      // Config field
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Config", GUILayout.Width(50));
      EditorGUI.BeginChangeCheck();
      State.Config = (WorldGeneratorConfigSO)EditorGUILayout.ObjectField(
        State.Config, typeof(WorldGeneratorConfigSO), false);
      if (EditorGUI.EndChangeCheck()) State.OnConfigChanged();
      if (GUILayout.Button("Find", GUILayout.Width(40))) State.FindConfig();
      EditorGUILayout.EndHorizontal();

      // Terrain field
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Terrain", GUILayout.Width(50));
      State.Terrain = (Terrain)EditorGUILayout.ObjectField(State.Terrain, typeof(Terrain), true);
      if (GUILayout.Button("Find", GUILayout.Width(40))) State.FindTerrain();
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.EndVertical();
    }
  }
}
#endif
