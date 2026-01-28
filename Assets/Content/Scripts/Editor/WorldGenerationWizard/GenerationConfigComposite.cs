#if UNITY_EDITOR
using System;
using Content.Scripts.World;
using Content.Scripts.World.Biomes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard {
  /// <summary>
  /// Main generation config page.
  /// Wraps WorldGeneratorConfigSO with convenient editing and generation controls.
  /// </summary>
  [Serializable]
  public class GenerationConfigComposite {
    private const string CONFIG_PATH = "Assets/Content/Resources/Environment/WorldGeneratorConfig.asset";

    // ═══════════════════════════════════════════════════════════════
    // CONFIG REFERENCE
    // ═══════════════════════════════════════════════════════════════

    [Title("World Generator Config")]
    [InlineEditor(InlineEditorModes.GUIOnly, Expanded = true)]
    [Required("Assign or create WorldGeneratorConfigSO")]
    [OnValueChanged("OnConfigChanged")]
    public WorldGeneratorConfigSO config;

    // ═══════════════════════════════════════════════════════════════
    // QUICK ACTIONS
    // ═══════════════════════════════════════════════════════════════

    [Title("Quick Actions")]
    [PropertyOrder(1)]
    [Button(ButtonSizes.Large, Icon = SdfIconType.PlayFill, Name = "Generate World"), GUIColor(0.4f, 0.9f, 0.4f)]
    [EnableIf("hasConfig")]
    private void GenerateWorld() {
      if (config == null) return;
      World.WorldGeneratorEditor.Generate(config);
    }

    [PropertyOrder(2)]
    [Button(ButtonSizes.Large, Icon = SdfIconType.Trash, Name = "Clear World"), GUIColor(1f, 0.5f, 0.5f)]
    [EnableIf("hasConfig")]
    private void ClearWorld() {
      if (config == null) return;
      if (!EditorUtility.DisplayDialog("Clear World", "Remove all generated objects?", "Clear", "Cancel")) return;
      World.WorldGeneratorEditor.Clear();
    }

    [Title("Preview")]
    [PropertyOrder(3)]
    [Button(Icon = SdfIconType.Eye, Name = "Preview Biomes"), GUIColor(0.6f, 0.8f, 1f)]
    [EnableIf("hasConfig")]
    private void PreviewBiomes() {
      if (config == null) return;
      
      var t = config.terrain != null ? config.terrain : Terrain.activeTerrain;
      if (t == null) {
        Debug.LogError("[WorldWizard] No terrain found");
        return;
      }

      var bounds = config.GetTerrainBounds(t);
      var s = config.Data.seed != 0 ? config.Data.seed : Environment.TickCount;
      config.cachedBiomeMap = VoronoiGenerator.Generate(bounds, config.Data.biomes, config.Data.biomeBorderBlend, s, config.Data.minBiomeCells, config.Data.maxBiomeCells);
      SceneView.RepaintAll();
      Debug.Log($"[WorldWizard] Biome preview: {config.cachedBiomeMap?.cells.Count ?? 0} cells");
    }

    [PropertyOrder(4)]
    [Button(Icon = SdfIconType.XCircle, Name = "Clear Preview")]
    [EnableIf("hasBiomePreview")]
    private void ClearPreview() {
      if (config == null) return;
      config.cachedBiomeMap = null;
      SceneView.RepaintAll();
    }

    // ═══════════════════════════════════════════════════════════════
    // SEED TOOLS
    // ═══════════════════════════════════════════════════════════════

    [Title("Seed Tools")]
    [PropertyOrder(10)]
    [Button("Random Seed", Icon = SdfIconType.Shuffle)]
    [EnableIf("hasConfig")]
    private void RandomizeSeed() {
      if (config == null) return;
      Undo.RecordObject(config, "Randomize Seed");
      config.Data.seed = UnityEngine.Random.Range(1, int.MaxValue);
      EditorUtility.SetDirty(config);
    }

    [PropertyOrder(11)]
    [Button("Reset Seed to 0", Icon = SdfIconType.ArrowCounterclockwise)]
    [EnableIf("hasConfig")]
    private void ResetSeed() {
      if (config == null) return;
      Undo.RecordObject(config, "Reset Seed");
      config.Data.seed = 0;
      EditorUtility.SetDirty(config);
    }

    // ═══════════════════════════════════════════════════════════════
    // TERRAIN TOOLS
    // ═══════════════════════════════════════════════════════════════

    [Title("Terrain Tools")]
    [PropertyOrder(20)]
    [Button("Apply Palette", Icon = SdfIconType.PaintBucket)]
    [EnableIf("hasPalette")]
    private void ApplyPalette() {
      if (config.Data?.terrainPalette == null) return;
      var t = config.terrain != null ? config.terrain : Terrain.activeTerrain;
      if (t != null) {
        Undo.RecordObject(t.terrainData, "Apply Terrain Palette");
        config.Data.terrainPalette.ApplyToTerrain(t);
      }
    }

    [PropertyOrder(21)]
    [Button("Flatten Terrain", Icon = SdfIconType.Subtract)]
    [EnableIf("hasConfig")]
    private void FlattenTerrain() {
      var t = config?.terrain != null ? config.terrain : Terrain.activeTerrain;
      if (t != null) {
        TerrainSculptor.Flatten(t, 0);
      }
    }

    [PropertyOrder(22)]
    [Button("Clear Splatmap", Icon = SdfIconType.Eraser)]
    [EnableIf("hasConfig")]
    private void ClearSplatmap() {
      var t = config?.terrain != null ? config.terrain : Terrain.activeTerrain;
      if (t != null) {
        SplatmapPainter.Clear(t, 0);
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════════

    [Title("Validation")]
    [PropertyOrder(30)]
    [Button("Validate Config", Icon = SdfIconType.CheckCircle)]
    [EnableIf("hasConfig")]
    private void ValidateConfig() {
      if (config == null) return;

      var errors = 0;

      if (config.Data.biomes == null || config.Data.biomes.Count == 0) {
        Debug.LogError("[WorldWizard] No biomes configured");
        errors++;
      } else {
        foreach (var biome in config.Data.biomes) {
          if (biome == null) {
            Debug.LogError("[WorldWizard] Null biome in list");
            errors++;
            continue;
          }

          if (biome.scatterConfigs != null) {
            foreach (var sc in biome.scatterConfigs) {
              if (sc?.rule == null) {
                Debug.LogError($"[WorldWizard] Null scatter rule in biome {biome.name}");
                errors++;
              }
            }
          }
        }
      }

      if (config.Data.terrainPalette == null) {
        Debug.LogWarning("[WorldWizard] No terrain palette assigned");
      }

      Debug.Log(errors == 0 ? "✓ Config valid" : $"✗ {errors} errors found");
    }

    // ═══════════════════════════════════════════════════════════════
    // ASSET MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    [Title("Asset Management")]
    [PropertyOrder(40)]
    [Button("Load Default Config", Icon = SdfIconType.FolderSymlink)]
    private void LoadDefault() {
      config = AssetDatabase.LoadAssetAtPath<WorldGeneratorConfigSO>(CONFIG_PATH);
      if (config == null) {
        Debug.LogWarning($"[WorldWizard] Config not found at {CONFIG_PATH}");
      }
    }

    [PropertyOrder(41)]
    [Button("Create New Config", Icon = SdfIconType.PlusCircle)]
    private void CreateNew() {
      var path = EditorUtility.SaveFilePanelInProject(
        "Create World Generator Config",
        "WorldGeneratorConfig",
        "asset",
        "Choose location for new config"
      );

      if (string.IsNullOrEmpty(path)) return;

      var newConfig = ScriptableObject.CreateInstance<WorldGeneratorConfigSO>();
      AssetDatabase.CreateAsset(newConfig, path);
      AssetDatabase.SaveAssets();
      config = newConfig;
      Selection.activeObject = newConfig;
    }

    [PropertyOrder(42)]
    [Button("Ping Config in Project", Icon = SdfIconType.Search)]
    [EnableIf("hasConfig")]
    private void PingConfig() {
      if (config != null) {
        EditorGUIUtility.PingObject(config);
        Selection.activeObject = config;
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // CONDITIONS
    // ═══════════════════════════════════════════════════════════════

    private bool hasConfig => config != null;
    private bool hasPalette => config != null && config.Data.terrainPalette != null;
    private bool hasBiomePreview => config != null && config.cachedBiomeMap != null;

    private void OnConfigChanged() { }

    public GenerationConfigComposite() {
      config = AssetDatabase.LoadAssetAtPath<WorldGeneratorConfigSO>(CONFIG_PATH);
    }
  }
}
#endif
