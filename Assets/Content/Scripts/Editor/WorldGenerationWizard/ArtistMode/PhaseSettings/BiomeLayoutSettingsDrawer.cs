#if UNITY_EDITOR
using Content.Scripts.World;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.PhaseSettings {
  /// <summary>
  /// Settings drawer for Biome Layout phase.
  /// Controls: cell count, border blend, domain warping.
  /// </summary>
  public class BiomeLayoutSettingsDrawer : IPhaseSettingsDrawer {
    public string PhaseName => "Biome Layout";
    public int PhaseIndex => 0;
    public bool IsFoldedOut { get; set; } = true;

    public void Draw(WorldGeneratorConfigSO config, GUIStyle boxStyle) {
      IsFoldedOut = EditorGUILayout.Foldout(IsFoldedOut, $"⚙ {PhaseName} Settings", true);
      if (!IsFoldedOut) return;

      EditorGUILayout.BeginVertical(boxStyle);

      var data = config.Data;

      // Cell Count
      EditorGUILayout.LabelField("Cell Count", EditorStyles.boldLabel);
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Min", GUILayout.Width(30));
      data.minBiomeCells = EditorGUILayout.IntSlider(data.minBiomeCells, 4, 50);
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Max", GUILayout.Width(30));
      data.maxBiomeCells = EditorGUILayout.IntSlider(data.maxBiomeCells, data.minBiomeCells, 100);
      EditorGUILayout.EndHorizontal();

      // Borders
      EditorGUILayout.Space(8);
      EditorGUILayout.LabelField("Borders", EditorStyles.boldLabel);
      data.biomeBorderBlend = EditorGUILayout.Slider("Blend Width", data.biomeBorderBlend, 5f, 50f);

      // Domain Warping
      EditorGUILayout.Space(8);
      EditorGUILayout.LabelField("Shape Noise (Domain Warping)", EditorStyles.boldLabel);
      data.useDomainWarping = EditorGUILayout.Toggle("Enable", data.useDomainWarping);

      if (data.useDomainWarping) {
        EditorGUI.indentLevel++;
        data.warpStrength = EditorGUILayout.Slider("Strength", data.warpStrength, 0f, 50f);
        data.warpScale = EditorGUILayout.Slider("Scale", data.warpScale, 0.001f, 0.1f);
        data.warpOctaves = EditorGUILayout.IntSlider("Octaves", data.warpOctaves, 1, 4);
        EditorGUI.indentLevel--;
      }

      EditorGUILayout.EndVertical();
    }
  }
}
#endif
