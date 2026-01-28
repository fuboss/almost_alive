#if UNITY_EDITOR
using System.Collections.Generic;
using Content.Scripts.World;
using Content.Scripts.World.Biomes;
using Content.Scripts.World.Vegetation;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.PhaseSettings {
  /// <summary>
  /// Settings drawer for Vegetation phase.
  /// Shows per-biome vegetation category controls with real-time preview.
  /// </summary>
  public class VegetationSettingsDrawer : IPhaseSettingsDrawer {
    public string PhaseName => "Vegetation";
    public int PhaseIndex => 3;
    public bool IsFoldedOut { get; set; } = true;

    private int _selectedBiomeIndex;
    private Dictionary<string, bool> _categoryFoldouts = new();
    private Vector2 _scrollPosition;

    public void Draw(WorldGeneratorConfigSO config, GUIStyle boxStyle) {
      IsFoldedOut = EditorGUILayout.Foldout(IsFoldedOut, $"⚙ {PhaseName} Settings", true);
      if (!IsFoldedOut) return;

      EditorGUILayout.BeginVertical(boxStyle);
      _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(350));
      
      var data = config.Data;
      
      // Master toggle
      data.paintVegetation = EditorGUILayout.Toggle("Paint Vegetation", data.paintVegetation);
      
      if (!data.paintVegetation) {
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        return;
      }

      EditorGUILayout.Space(8);
      
      // Biome selector
      if (data.biomes == null || data.biomes.Count == 0) {
        EditorGUILayout.HelpBox("No biomes configured", MessageType.Warning);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        return;
      }

      var biomeNames = new string[data.biomes.Count];
      for (int i = 0; i < data.biomes.Count; i++) {
        biomeNames[i] = data.biomes[i] != null ? data.biomes[i].name : $"[{i}] null";
      }

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Biome", GUILayout.Width(50));
      _selectedBiomeIndex = EditorGUILayout.Popup(_selectedBiomeIndex, biomeNames);
      EditorGUILayout.EndHorizontal();

      var selectedBiome = data.biomes[_selectedBiomeIndex];
      if (selectedBiome == null) {
        EditorGUILayout.HelpBox("Selected biome is null", MessageType.Error);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        return;
      }

      EditorGUILayout.Space(4);
      
      // Draw biome vegetation config
      DrawBiomeVegetationConfig(selectedBiome);

      EditorGUILayout.Space(8);
      
      // Quick actions
      EditorGUILayout.EndScrollView();
      
      EditorGUILayout.Space(4);
      
      // Quick actions (outside scroll)
      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Initialize Defaults", GUILayout.Height(24))) {
        selectedBiome.vegetationConfig.InitializeDefaults();
        EditorUtility.SetDirty(selectedBiome);
      }
      if (GUILayout.Button("Clear Cache", GUILayout.Height(24))) {
        VegetationPainter.ClearMaskCache();
        Debug.Log("[Vegetation] Mask cache cleared");
      }
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.EndVertical();
    }

    private void DrawBiomeVegetationConfig(BiomeSO biome) {
      var vegConfig = biome.vegetationConfig;
      
      EditorGUI.BeginChangeCheck();
      
      // Global settings
      EditorGUILayout.LabelField("Global", EditorStyles.boldLabel);
      vegConfig.globalDensity = EditorGUILayout.Slider("Global Density", vegConfig.globalDensity, 0f, 3f);
      vegConfig.maxDensityPerCell = EditorGUILayout.IntSlider("Max Per Cell", vegConfig.maxDensityPerCell, 8, 255);

      EditorGUILayout.Space(8);
      
      // Categories
      if (vegConfig.categories == null || vegConfig.categories.Length == 0) {
        EditorGUILayout.HelpBox("No vegetation categories. Click 'Initialize Defaults' to add.", MessageType.Info);
      } else {
        foreach (var category in vegConfig.categories) {
          DrawCategory(category, biome);
        }
      }

      if (EditorGUI.EndChangeCheck()) {
        EditorUtility.SetDirty(biome);
      }
    }

    private void DrawCategory(VegetationCategory category, BiomeSO biome) {
      var key = $"{biome.name}_{category.name}";
      if (!_categoryFoldouts.ContainsKey(key)) {
        _categoryFoldouts[key] = true;
      }

      var icon = category.size switch {
        VegetationSize.Small => "🌱",
        VegetationSize.Medium => "🌿",
        VegetationSize.Large => "🌳",
        _ => "🌾"
      };

      var headerStyle = new GUIStyle(EditorStyles.foldoutHeader);
      var bgColor = category.enabled ? new Color(0.3f, 0.5f, 0.3f, 0.3f) : new Color(0.5f, 0.3f, 0.3f, 0.3f);
      
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      
      // Category header
      EditorGUILayout.BeginHorizontal();
      _categoryFoldouts[key] = EditorGUILayout.Foldout(_categoryFoldouts[key], $"{icon} {category.name}", true);
      category.enabled = EditorGUILayout.Toggle(category.enabled, GUILayout.Width(20));
      EditorGUILayout.EndHorizontal();

      if (_categoryFoldouts[key] && category.enabled) {
        EditorGUI.indentLevel++;
        
        // Basic settings
        category.densityMultiplier = EditorGUILayout.Slider("Density Mult", category.densityMultiplier, 0f, 2f);
        
        // Noise settings (inline)
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Placement Noise", EditorStyles.miniLabel);
        
        var noise = category.noise;
        noise.scale = EditorGUILayout.Slider("Scale", noise.scale, 0.001f, 0.2f);
        noise.threshold = EditorGUILayout.Slider("Threshold", noise.threshold, 0f, 1f);
        noise.blend = EditorGUILayout.Slider("Blend", noise.blend, 0f, 0.5f);
        noise.octaves = EditorGUILayout.IntSlider("Octaves", noise.octaves, 1, 6);
        
        // Layers count
        EditorGUILayout.Space(4);
        var layerCount = category.layers?.Length ?? 0;
        EditorGUILayout.LabelField($"Layers: {layerCount}", EditorStyles.miniLabel);
        
        EditorGUI.indentLevel--;
      }
      
      EditorGUILayout.EndVertical();
    }
  }
}
#endif
