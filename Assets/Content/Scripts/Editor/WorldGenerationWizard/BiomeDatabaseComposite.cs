#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Scripts.World.Biomes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard {
  /// <summary>
  /// Database page for managing biomes.
  /// Scan, create, edit, and delete BiomeSO assets.
  /// </summary>
  [Serializable]
  public class BiomeDatabaseComposite {
    private const string BIOMES_FOLDER = "Assets/Content/Resources/Environment/Biomes";

    // ═══════════════════════════════════════════════════════════════
    // DATABASE
    // ═══════════════════════════════════════════════════════════════

    [Title("Biomes Database")]
    [InfoBox("Click asset to select for editing below")]
    [OnInspectorInit("RefreshDatabase")]
    [InlineButton("RefreshDatabase", "↻")]
    [ListDrawerSettings(
      ShowFoldout = false, 
      DraggableItems = false, 
      HideAddButton = true, 
      HideRemoveButton = true,
      OnTitleBarGUI = "DrawListHeader"
    )]
    public List<BiomeSO> biomes = new();

    private void DrawListHeader() {
      if (GUILayout.Button("Open Folder", EditorStyles.miniButton, GUILayout.Width(80))) {
        OpenFolder();
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // SELECTED BIOME
    // ═══════════════════════════════════════════════════════════════

    [Title("Selected Biome")]
    [InlineEditor(InlineEditorModes.GUIOnly, Expanded = true)]
    public BiomeSO selectedBiome;

    [ShowIf("@selectedBiome != null")]
    [Button("Ping Asset", Icon = SdfIconType.Search)]
    private void PingSelected() {
      if (selectedBiome != null) {
        EditorGUIUtility.PingObject(selectedBiome);
        Selection.activeObject = selectedBiome;
      }
    }

    [ShowIf("@selectedBiome != null")]
    [Button("Delete Selected", Icon = SdfIconType.Trash), GUIColor(1f, 0.5f, 0.5f)]
    private void DeleteSelected() {
      if (selectedBiome == null) return;
      if (!EditorUtility.DisplayDialog("Delete Biome",
            $"Delete '{selectedBiome.name}'?\n\nThis cannot be undone.", "Delete", "Cancel")) return;

      var path = AssetDatabase.GetAssetPath(selectedBiome);
      AssetDatabase.DeleteAsset(path);
      selectedBiome = null;
      RefreshDatabase();
      Debug.Log($"[BiomeDB] Deleted: {path}");
    }

    // ═══════════════════════════════════════════════════════════════
    // CREATE NEW BIOME
    // ═══════════════════════════════════════════════════════════════

    [Title("Create New Biome")]
    [FoldoutGroup("Create", Expanded = false)]
    [LabelText("Biome Type")]
    public BiomeType newBiomeType = BiomeType.Forest;

    [FoldoutGroup("Create")]
    [LabelText("Name Suffix (optional)")]
    public string newBiomeSuffix = "";

    [FoldoutGroup("Create")]
    [LabelText("Debug Color")]
    public Color newBiomeColor = Color.green;

    [FoldoutGroup("Create")]
    [Button("Create Biome", Icon = SdfIconType.PlusCircle), GUIColor(0.4f, 0.9f, 0.4f)]
    private void CreateBiome() {
      var typeName = newBiomeType.ToString();
      var fileName = string.IsNullOrWhiteSpace(newBiomeSuffix)
        ? $"Biome_{typeName}"
        : $"Biome_{typeName}_{newBiomeSuffix}";

      var path = $"{BIOMES_FOLDER}/{fileName}.asset";

      if (File.Exists(path)) {
        EditorUtility.DisplayDialog("Error", $"Biome already exists:\n{path}", "OK");
        return;
      }

      EnsureFolderExists();

      var biome = ScriptableObject.CreateInstance<BiomeSO>();
      biome.type = newBiomeType;
      biome.debugColor = newBiomeColor;
      biome.weight = 1f;

      AssetDatabase.CreateAsset(biome, path);
      AssetDatabase.SaveAssets();
      
      RefreshDatabase();
      
      selectedBiome = biome;
      EditorGUIUtility.PingObject(biome);
      Debug.Log($"[BiomeDB] Created: {fileName}");
    }

    // ═══════════════════════════════════════════════════════════════
    // ACTIONS
    // ═══════════════════════════════════════════════════════════════

    [Title("Validation")]
    [Button("Validate All Biomes", Icon = SdfIconType.CheckCircle)]
    private void ValidateAll() {
      var errors = 0;
      foreach (var biome in biomes) {
        if (biome == null) {
          errors++;
          continue;
        }

        if (biome.GetBaseLayerIndex() < 0) {
          Debug.LogError($"[BiomeDB] {biome.name}: Invalid base texture layer");
          errors++;
        }
      }

      Debug.Log(errors == 0 ? "✓ All biomes valid" : $"✗ {errors} issues found");
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    public void RefreshDatabase() {
      biomes.Clear();

      if (!Directory.Exists(BIOMES_FOLDER)) {
        Debug.LogWarning($"[BiomeDB] Folder not found: {BIOMES_FOLDER}");
        return;
      }

      var guids = AssetDatabase.FindAssets("t:BiomeSO", new[] { BIOMES_FOLDER });
      foreach (var guid in guids) {
        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
        var biome = AssetDatabase.LoadAssetAtPath<BiomeSO>(assetPath);
        if (biome != null) {
          biomes.Add(biome);
        }
      }

      biomes = biomes.OrderBy(b => b.type).ThenBy(b => b.name).ToList();
    }

    private void OpenFolder() {
      EnsureFolderExists();
      var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(BIOMES_FOLDER);
      if (obj != null) {
        EditorGUIUtility.PingObject(obj);
        Selection.activeObject = obj;
      }
    }

    private void EnsureFolderExists() {
      if (!Directory.Exists(BIOMES_FOLDER)) {
        Directory.CreateDirectory(BIOMES_FOLDER);
        AssetDatabase.Refresh();
      }
    }
  }
}
#endif
