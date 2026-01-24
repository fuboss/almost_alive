#if UNITY_EDITOR
using System.IO;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.EditorUtilities;
using Content.Scripts.Building.Runtime;
using Content.Scripts.Descriptors.Tags;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Content.Scripts.Building.Editor {
  [CustomEditor(typeof(StructureFoundationBuilder))]
  public class StructureFoundationBuilderEditor : UnityEditor.Editor {
    private const string PrefabsPath = "Assets/Content/Prefabs/BuildingStructures";
    private const string AddressableGroupName = "BuildingStructures";  // addressable group
    private const string StructureLabel = "Structure";

    private StructureFoundationBuilder _builder;
    private SlotType _createSlotsType = SlotType.Utility;

    private void OnEnable() {
      _builder = (StructureFoundationBuilder)target;
    }

    public override void OnInspectorGUI() {
      // Draw default Odin inspector
      base.OnInspectorGUI();

      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

      // Row 1: Snap + Add Slot
      EditorGUILayout.BeginHorizontal();
      
      if (GUILayout.Button("Snap to Grid", GUILayout.Height(25))) {
        SnapToGrid();
      }

      if (GUILayout.Button("Add Slot", GUILayout.Height(25))) {
        AddSlotAtCenter();
      }
      
      EditorGUILayout.EndHorizontal();

      // Row 2: Mirror + Validate
      EditorGUILayout.BeginHorizontal();
      
      if (GUILayout.Button("Mirror X", GUILayout.Height(25))) {
        MirrorSlotsX();
      }

      if (GUILayout.Button("Validate", GUILayout.Height(25))) {
        RunValidation();
      }
      
      EditorGUILayout.EndHorizontal();

      // Row 3: Create Slots for all cells
      EditorGUILayout.Space(5);
      EditorGUILayout.BeginHorizontal();
      
      EditorGUILayout.LabelField("Default Type:", GUILayout.Width(80));
      _createSlotsType = (SlotType)EditorGUILayout.EnumPopup(_createSlotsType, GUILayout.Width(100));
      
      if (GUILayout.Button("Create Slots (All Cells)", GUILayout.Height(22))) {
        CreateSlotsForAllCells();
      }
      
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space(5);

      // Terrain info
      if (_builder.drawTerrainCheck) {
        DrawTerrainInfo();
      }

      EditorGUILayout.Space(10);
      
      // Export button
      GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
      if (GUILayout.Button("Save as Addressable Prefab", GUILayout.Height(30))) {
        SaveAsAddressablePrefab();
      }
      GUI.backgroundColor = Color.white;
    }

    private void SnapToGrid() {
      Undo.RecordObject(_builder.transform, "Snap to Grid");
      
      var pos = _builder.transform.position;
      var cellSize = Content.Scripts.World.Grid.WorldGrid.cellSize;
      
      pos.x = Mathf.Round(pos.x / cellSize) * cellSize;
      pos.z = Mathf.Round(pos.z / cellSize) * cellSize;
      
      _builder.transform.position = pos;
      
      EditorUtility.SetDirty(_builder);
      Debug.Log($"[StructureFoundationBuilder] Snapped to grid: {pos}");
    }

    private void AddSlotAtCenter() {
      Undo.RecordObject(_builder, "Add Slot");
      
      var cellSize = Content.Scripts.World.Grid.WorldGrid.cellSize;
      var centerPos = new Vector3(
        _builder.footprint.x * cellSize * 0.5f,
        0,
        _builder.footprint.y * cellSize * 0.5f
      );
      
      _builder.AddSlot(centerPos, SlotType.Utility);
      
      EditorUtility.SetDirty(_builder);
      Debug.Log("[StructureFoundationBuilder] Added slot at center");
    }

    private void MirrorSlotsX() {
      Undo.RecordObject(_builder, "Mirror Slots X");
      
      var countBefore = _builder.slots.Count;
      _builder.MirrorSlotsX();
      var countAfter = _builder.slots.Count;
      
      EditorUtility.SetDirty(_builder);
      Debug.Log($"[StructureFoundationBuilder] Mirrored slots: {countBefore} -> {countAfter}");
    }

    private void CreateSlotsForAllCells() {
      if (_builder.slots.Count > 0) {
        if (!EditorUtility.DisplayDialog("Replace Slots?", 
          $"This will replace {_builder.slots.Count} existing slots with {_builder.footprint.x * _builder.footprint.y} new slots.\n\nContinue?", 
          "Yes", "Cancel")) {
          return;
        }
      }

      Undo.RecordObject(_builder, "Create Slots For All Cells");
      
      _builder.CreateSlotsForAllCells(_createSlotsType);
      
      EditorUtility.SetDirty(_builder);
      Debug.Log($"[StructureFoundationBuilder] Created {_builder.slots.Count} slots");
    }

    private void RunValidation() {
      var errors = _builder.Validate();
      
      if (errors.Count == 0) {
        Debug.Log("[StructureFoundationBuilder] Validation passed ✓");
        EditorUtility.DisplayDialog("Validation", "All checks passed!", "OK");
      }
      else {
        foreach (var error in errors) {
          Debug.LogWarning($"[StructureFoundationBuilder] {error}");
        }
        EditorUtility.DisplayDialog("Validation Failed", 
          $"Found {errors.Count} issue(s):\n\n• " + string.Join("\n• ", errors), 
          "OK");
      }
    }

    private void DrawTerrainInfo() {
      var (min, max, variance) = _builder.GetTerrainVariance();
      
      EditorGUILayout.Space(5);
      EditorGUILayout.LabelField("Terrain Analysis", EditorStyles.boldLabel);
      
      EditorGUI.indentLevel++;
      EditorGUILayout.LabelField($"Min Height: {min:F2}");
      EditorGUILayout.LabelField($"Max Height: {max:F2}");
      
      var isWarning = variance > _builder.maxHeightVariance;
      var oldColor = GUI.color;
      GUI.color = isWarning ? Color.red : Color.green;
      EditorGUILayout.LabelField($"Variance: {variance:F2}" + (isWarning ? " ⚠️ EXCEEDS THRESHOLD" : " ✓"));
      GUI.color = oldColor;
      
      EditorGUI.indentLevel--;
    }

    private void SaveAsAddressablePrefab() {
      // Validate first
      var errors = _builder.Validate();
      if (errors.Count > 0) {
        EditorUtility.DisplayDialog("Cannot Save Prefab", 
          "Please fix validation errors first:\n\n• " + string.Join("\n• ", errors), 
          "OK");
        return;
      }

      // Ensure directory exists
      if (!Directory.Exists(PrefabsPath)) {
        Directory.CreateDirectory(PrefabsPath);
        AssetDatabase.Refresh();
      }

      // Generate prefab name
      var prefabName = string.IsNullOrEmpty(_builder.gameObject.name) 
        ? "NewStructure" 
        : _builder.gameObject.name;
      
      var prefabPath = $"{PrefabsPath}/{prefabName}.prefab";

      // Check if exists
      if (File.Exists(prefabPath)) {
        if (!EditorUtility.DisplayDialog("Overwrite?", 
          $"Prefab '{prefabName}' already exists.\n\nOverwrite?", 
          "Yes", "Cancel")) {
          return;
        }
      }

      // Remove StructureFoundationBuilder before saving (it's editor-only tool)
      // We need to save without this component
      var tempGO = Instantiate(_builder.gameObject);
      tempGO.name = prefabName;
      
      // Remove the builder component from temp copy
      var builderOnCopy = tempGO.GetComponent<StructureFoundationBuilder>();
      if (builderOnCopy != null) {
        DestroyImmediate(builderOnCopy);
      }

      // Add StructureDescription with metadata
      var description = tempGO.AddComponent<StructureTag>();
      description.footprint = _builder.footprint;
      description.entryDirections = _builder.entryDirections;
      description.slotCount = _builder.slots.Count;
      
      // Copy slots
      description.slots = new SlotDefinition[_builder.slots.Count];
      for (var i = 0; i < _builder.slots.Count; i++) {
        var src = _builder.slots[i];
        description.slots[i] = new SlotDefinition {
          slotId = src.slotId,
          type = src.type,
          localPosition = src.localPosition,
          localRotation = src.localRotation,
          acceptedModuleTags = src.acceptedModuleTags?.Clone() as string[],
          isInterior = src.isInterior,
          startsLocked = src.startsLocked
        };
      }

      // Save prefab
      var prefab = PrefabUtility.SaveAsPrefabAsset(tempGO, prefabPath);
      
      // Cleanup temp object
      DestroyImmediate(tempGO);

      if (prefab == null) {
        EditorUtility.DisplayDialog("Error", "Failed to create prefab!", "OK");
        return;
      }

      // Make it addressable
      var success = MakeAddressable(prefabPath, prefabName);

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      // Select the prefab
      Selection.activeObject = prefab;
      EditorGUIUtility.PingObject(prefab);

      var message = success 
        ? $"Prefab saved and added to Addressables:\n{prefabPath}\n\nGroup: {AddressableGroupName}\nLabel: {StructureLabel}"
        : $"Prefab saved:\n{prefabPath}\n\n⚠️ Failed to add to Addressables. Please add manually.";

      Debug.Log($"[StructureFoundationBuilder] Saved: {prefabPath}");
      EditorUtility.DisplayDialog("Prefab Saved", message, "OK");
    }

    private bool MakeAddressable(string assetPath, string address) {
      var settings = AddressableAssetSettingsDefaultObject.Settings;
      if (settings == null) {
        Debug.LogError("[StructureFoundationBuilder] Addressable settings not found!");
        return false;
      }

      // Find or create group
      var group = settings.FindGroup(AddressableGroupName);
      if (group == null) {
        Debug.LogWarning($"[StructureFoundationBuilder] Group '{AddressableGroupName}' not found. Creating new group.");
        group = settings.CreateGroup(AddressableGroupName, false, false, true, null, typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema));
      }

      // Get asset GUID
      var guid = AssetDatabase.AssetPathToGUID(assetPath);
      if (string.IsNullOrEmpty(guid)) {
        Debug.LogError($"[StructureFoundationBuilder] Could not find GUID for {assetPath}");
        return false;
      }

      // Create or update entry
      var entry = settings.CreateOrMoveEntry(guid, group, false, false);
      if (entry == null) {
        Debug.LogError("[StructureFoundationBuilder] Failed to create addressable entry");
        return false;
      }

      // Set address
      entry.address = address;

      // Add label
      if (!settings.GetLabels().Contains(StructureLabel)) {
        settings.AddLabel(StructureLabel);
      }
      entry.SetLabel(StructureLabel, true, true);

      Debug.Log($"[StructureFoundationBuilder] Added to Addressables: {address} (Group: {group.Name}, Label: {StructureLabel})");
      return true;
    }
  }
}
#endif
