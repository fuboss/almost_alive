#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Scripts.World.Vegetation;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard {
  /// <summary>
  /// Database page for managing vegetation prototypes.
  /// Scan, create, edit, and delete VegetationPrototypeSO assets.
  /// </summary>
  [Serializable]
  public class VegetationDatabaseComposite {
    private const string VEGETATION_FOLDER = "Assets/Content/Resources/Environment/Vegetation";

    // ═══════════════════════════════════════════════════════════════
    // DATABASE
    // ═══════════════════════════════════════════════════════════════

    [Title("Vegetation Database")]
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
    public List<VegetationPrototypeSO> prototypes = new();

    private void DrawListHeader() {
      if (GUILayout.Button("Open Folder", EditorStyles.miniButton, GUILayout.Width(80))) {
        OpenFolder();
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // SELECTED PROTOTYPE
    // ═══════════════════════════════════════════════════════════════

    [Title("Selected Vegetation")]
    [InlineEditor(InlineEditorModes.GUIOnly, Expanded = true)]
    public VegetationPrototypeSO selectedPrototype;

    [ShowIf("@selectedPrototype != null")]
    [Button("Ping Asset", Icon = SdfIconType.Search)]
    private void PingSelected() {
      if (selectedPrototype != null) {
        EditorGUIUtility.PingObject(selectedPrototype);
        Selection.activeObject = selectedPrototype;
      }
    }

    [ShowIf("@selectedPrototype != null")]
    [Button("Delete Selected", Icon = SdfIconType.Trash), GUIColor(1f, 0.5f, 0.5f)]
    private void DeleteSelected() {
      if (selectedPrototype == null) return;
      if (!EditorUtility.DisplayDialog("Delete Vegetation Prototype",
            $"Delete '{selectedPrototype.name}'?\n\nThis cannot be undone.", "Delete", "Cancel")) return;

      var path = AssetDatabase.GetAssetPath(selectedPrototype);
      AssetDatabase.DeleteAsset(path);
      selectedPrototype = null;
      RefreshDatabase();
      Debug.Log($"[VegetationDB] Deleted: {path}");
    }

    // ═══════════════════════════════════════════════════════════════
    // CREATE NEW PROTOTYPE
    // ═══════════════════════════════════════════════════════════════

    [Title("Create New Vegetation Prototype")]
    [FoldoutGroup("Create", Expanded = false)]
    [LabelText("Name")]
    public string newPrototypeName = "";

    [FoldoutGroup("Create")]
    [LabelText("Prefab")]
    [AssetsOnly, Required]
    public GameObject newPrefab;

    [FoldoutGroup("Create")]
    [LabelText("Render Mode")]
    public DetailRenderMode newRenderMode = DetailRenderMode.VertexLit;

    [FoldoutGroup("Create")]
    [LabelText("Width Range")]
    [MinMaxSlider(0.1f, 5f, true)]
    public Vector2 newWidthRange = new(0.5f, 1.5f);

    [FoldoutGroup("Create")]
    [LabelText("Height Range")]
    [MinMaxSlider(0.1f, 5f, true)]
    public Vector2 newHeightRange = new(0.5f, 1.5f);

    [FoldoutGroup("Create")]
    [LabelText("Dry Color")]
    public Color newDryColor = new(0.8f, 0.7f, 0.4f);

    [FoldoutGroup("Create")]
    [LabelText("Healthy Color")]
    public Color newHealthyColor = new(0.3f, 0.8f, 0.2f);

    [FoldoutGroup("Create")]
    [Button("Create Prototype", Icon = SdfIconType.PlusCircle), GUIColor(0.4f, 0.9f, 0.4f)]
    [EnableIf("canCreate")]
    private void CreatePrototype() {
      if (string.IsNullOrWhiteSpace(newPrototypeName)) {
        EditorUtility.DisplayDialog("Error", "Name cannot be empty", "OK");
        return;
      }

      var fileName = $"VP_{newPrototypeName}";
      var path = $"{VEGETATION_FOLDER}/{fileName}.asset";

      if (File.Exists(path)) {
        EditorUtility.DisplayDialog("Error", $"Prototype already exists:\n{path}", "OK");
        return;
      }

      EnsureFolderExists();

      var prototype = ScriptableObject.CreateInstance<VegetationPrototypeSO>();
      prototype.prefab = newPrefab;
      prototype.renderMode = newRenderMode;
      prototype.widthRange = newWidthRange;
      prototype.heightRange = newHeightRange;
      prototype.dryColor = newDryColor;
      prototype.healthyColor = newHealthyColor;
      prototype.useInstancing = true;

      AssetDatabase.CreateAsset(prototype, path);
      AssetDatabase.SaveAssets();

      RefreshDatabase();

      selectedPrototype = prototype;
      EditorGUIUtility.PingObject(prototype);

      newPrototypeName = "";
      newPrefab = null;

      Debug.Log($"[VegetationDB] Created: {fileName}");
    }

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION & BATCH
    // ═══════════════════════════════════════════════════════════════

    [Title("Tools")]
    [ButtonGroup("Tools")]
    [Button("Validate All", Icon = SdfIconType.CheckCircle)]
    private void ValidateAll() {
      var errors = 0;
      foreach (var prototype in prototypes) {
        if (prototype == null) {
          errors++;
          continue;
        }

        if (prototype.prefab == null && (prototype.prefabs == null || prototype.prefabs.Length == 0)) {
          Debug.LogError($"[VegetationDB] {prototype.name}: No prefab assigned");
          errors++;
        }
      }

      Debug.Log(errors == 0 ? "✓ All vegetation prototypes valid" : $"✗ {errors} issues found");
    }

    [ButtonGroup("Tools")]
    [Button("Enable Instancing All", Icon = SdfIconType.Lightning)]
    private void ApplyInstancingToAll() {
      var count = 0;
      foreach (var prototype in prototypes) {
        if (prototype != null && !prototype.useInstancing) {
          Undo.RecordObject(prototype, "Enable Instancing");
          prototype.useInstancing = true;
          EditorUtility.SetDirty(prototype);
          count++;
        }
      }
      Debug.Log($"[VegetationDB] Enabled instancing on {count} prototypes");
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private bool canCreate => !string.IsNullOrWhiteSpace(newPrototypeName) && newPrefab != null;

    public void RefreshDatabase() {
      prototypes.Clear();

      if (!Directory.Exists(VEGETATION_FOLDER)) {
        Debug.LogWarning($"[VegetationDB] Folder not found: {VEGETATION_FOLDER}");
        return;
      }

      var guids = AssetDatabase.FindAssets("t:VegetationPrototypeSO", new[] { VEGETATION_FOLDER });
      foreach (var guid in guids) {
        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
        var prototype = AssetDatabase.LoadAssetAtPath<VegetationPrototypeSO>(assetPath);
        if (prototype != null) {
          prototypes.Add(prototype);
        }
      }

      prototypes = prototypes.OrderBy(p => p.name).ToList();
    }

    private void OpenFolder() {
      EnsureFolderExists();
      var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(VEGETATION_FOLDER);
      if (obj != null) {
        EditorGUIUtility.PingObject(obj);
        Selection.activeObject = obj;
      }
    }

    private void EnsureFolderExists() {
      if (!Directory.Exists(VEGETATION_FOLDER)) {
        Directory.CreateDirectory(VEGETATION_FOLDER);
        AssetDatabase.Refresh();
      }
    }
  }
}
#endif
