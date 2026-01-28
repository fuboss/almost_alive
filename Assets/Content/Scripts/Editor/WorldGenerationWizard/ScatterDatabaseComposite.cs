#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Scripts.World;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard {
  /// <summary>
  /// Database page for managing scatter rules.
  /// Scan, create, edit, and delete ScatterRuleSO assets.
  /// </summary>
  [Serializable]
  public class ScatterDatabaseComposite {
    private const string SCATTERS_FOLDER = "Assets/Content/Resources/Environment/Scatters";

    // ═══════════════════════════════════════════════════════════════
    // DATABASE
    // ═══════════════════════════════════════════════════════════════

    [Title("Scatters Database")]
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
    public List<ScatterRuleSO> scatters = new();

    private void DrawListHeader() {
      if (GUILayout.Button("Open Folder", EditorStyles.miniButton, GUILayout.Width(80))) {
        OpenFolder();
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // SELECTED SCATTER
    // ═══════════════════════════════════════════════════════════════

    [Title("Selected Scatter Rule")]
    [InlineEditor(InlineEditorModes.GUIOnly, Expanded = true)]
    public ScatterRuleSO selectedScatter;

    [ShowIf("@selectedScatter != null")]
    [Button("Ping Asset", Icon = SdfIconType.Search)]
    private void PingSelected() {
      if (selectedScatter != null) {
        EditorGUIUtility.PingObject(selectedScatter);
        Selection.activeObject = selectedScatter;
      }
    }

    [ShowIf("@selectedScatter != null")]
    [Button("Delete Selected", Icon = SdfIconType.Trash), GUIColor(1f, 0.5f, 0.5f)]
    private void DeleteSelected() {
      if (selectedScatter == null) return;
      if (!EditorUtility.DisplayDialog("Delete Scatter Rule",
            $"Delete '{selectedScatter.name}'?\n\nThis cannot be undone.", "Delete", "Cancel")) return;

      var path = AssetDatabase.GetAssetPath(selectedScatter);
      AssetDatabase.DeleteAsset(path);
      selectedScatter = null;
      RefreshDatabase();
      Debug.Log($"[ScatterDB] Deleted: {path}");
    }

    // ═══════════════════════════════════════════════════════════════
    // CREATE NEW SCATTER
    // ═══════════════════════════════════════════════════════════════

    [Title("Create New Scatter Rule")]
    [FoldoutGroup("Create", Expanded = false)]
    [LabelText("Rule Name")]
    public string newScatterName = "";

    [FoldoutGroup("Create")]
    [LabelText("Actor Key")]
    [ValueDropdown("GetActorKeys")]
    public string newActorKey = "";

    [FoldoutGroup("Create")]
    [LabelText("Or Prefab")]
    [AssetsOnly]
    public GameObject newPrefab;

    [FoldoutGroup("Create")]
    [LabelText("Density")]
    [Range(0.01f, 10f)]
    public float newDensity = 0.5f;

    [FoldoutGroup("Create")]
    [LabelText("Min Spacing")]
    [Range(1f, 50f)]
    public float newMinSpacing = 5f;

    [FoldoutGroup("Create")]
    [Button("Create Scatter Rule", Icon = SdfIconType.PlusCircle), GUIColor(0.4f, 0.9f, 0.4f)]
    [EnableIf("canCreate")]
    private void CreateScatter() {
      if (string.IsNullOrWhiteSpace(newScatterName)) {
        EditorUtility.DisplayDialog("Error", "Name cannot be empty", "OK");
        return;
      }

      var fileName = $"ScatterRule_{newScatterName}";
      var path = $"{SCATTERS_FOLDER}/{fileName}.asset";

      if (File.Exists(path)) {
        EditorUtility.DisplayDialog("Error", $"Scatter rule already exists:\n{path}", "OK");
        return;
      }

      EnsureFolderExists();

      var scatter = ScriptableObject.CreateInstance<ScatterRuleSO>();
      scatter.actorKey = newActorKey;
      scatter.prefab = newPrefab;
      scatter.density = newDensity;
      scatter.minSpacing = newMinSpacing;

      AssetDatabase.CreateAsset(scatter, path);
      AssetDatabase.SaveAssets();

      RefreshDatabase();

      selectedScatter = scatter;
      EditorGUIUtility.PingObject(scatter);
      
      newScatterName = "";
      newActorKey = "";
      newPrefab = null;
      
      Debug.Log($"[ScatterDB] Created: {fileName}");
    }

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════════

    [Title("Validation")]
    [Button("Validate All Scatters", Icon = SdfIconType.CheckCircle)]
    private void ValidateAll() {
      var errors = 0;
      foreach (var scatter in scatters) {
        if (scatter == null) {
          errors++;
          continue;
        }

        if (string.IsNullOrEmpty(scatter.actorKey) && scatter.prefab == null) {
          Debug.LogError($"[ScatterDB] {scatter.name}: No actorKey or prefab assigned");
          errors++;
        }
      }

      Debug.Log(errors == 0 ? "✓ All scatter rules valid" : $"✗ {errors} issues found");
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private bool canCreate => !string.IsNullOrWhiteSpace(newScatterName) && 
                               (!string.IsNullOrWhiteSpace(newActorKey) || newPrefab != null);

    public void RefreshDatabase() {
      scatters.Clear();

      if (!Directory.Exists(SCATTERS_FOLDER)) {
        Debug.LogWarning($"[ScatterDB] Folder not found: {SCATTERS_FOLDER}");
        return;
      }

      var guids = AssetDatabase.FindAssets("t:ScatterRuleSO", new[] { SCATTERS_FOLDER });
      foreach (var guid in guids) {
        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
        var scatter = AssetDatabase.LoadAssetAtPath<ScatterRuleSO>(assetPath);
        if (scatter != null) {
          scatters.Add(scatter);
        }
      }

      scatters = scatters.OrderBy(s => s.actorName).ToList();
    }

    private void OpenFolder() {
      EnsureFolderExists();
      var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SCATTERS_FOLDER);
      if (obj != null) {
        EditorGUIUtility.PingObject(obj);
        Selection.activeObject = obj;
      }
    }

    private void EnsureFolderExists() {
      if (!Directory.Exists(SCATTERS_FOLDER)) {
        Directory.CreateDirectory(SCATTERS_FOLDER);
        AssetDatabase.Refresh();
      }
    }

    private static IEnumerable<string> GetActorKeys() {
      yield return "";
      foreach (var key in AI.GOAP.GOAPEditorHelper.GetActorKeys()) {
        yield return key;
      }
    }
  }
}
#endif
