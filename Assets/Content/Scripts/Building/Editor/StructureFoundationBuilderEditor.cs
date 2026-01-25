#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.EditorUtilities;
using Content.Scripts.Descriptors.Tags;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Content.Scripts.Building.Editor {
  [CustomEditor(typeof(StructureFoundationBuilder))]
  public class StructureFoundationBuilderEditor : OdinEditor {
    private const string PrefabsPath = "Assets/Content/Prefabs/BuildingStructures";
    private const string AddressableGroupName = "BuildingStructures";
    private const string StructureLabel = "Structure";

    private StructureFoundationBuilder _builder;
    private SlotType _createSlotsType = SlotType.Utility;
    private SlotType _paintSlotType = SlotType.Utility;
    private bool _paintMode;
    private Vector2Int _hoveredCell = new(-1, -1);

    private static readonly Color HoverColor = new(1f, 1f, 1f, 0.3f);
    private static readonly Color HoverOccupiedColor = new(1f, 0.3f, 0.3f, 0.3f);

    protected override void OnEnable() {
      base.OnEnable();
      _builder = (StructureFoundationBuilder)target;
      SceneView.duringSceneGui += OnSceneGUI;
    }

    protected override void OnDisable() {
      base.OnDisable();
      SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView) {
      if (_builder == null || !_paintMode) return;

      HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

      var e = Event.current;
      var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

      var groundPlane = new Plane(Vector3.up, _builder.transform.position);
      if (!groundPlane.Raycast(ray, out var distance)) return;

      var hitPoint = ray.GetPoint(distance);
      var cell = _builder.WorldToCell(hitPoint);

      _hoveredCell = _builder.IsCellInBounds(cell.x, cell.y) ? cell : new Vector2Int(-1, -1);

      if (_hoveredCell.x >= 0) {
        DrawCellHighlight(_hoveredCell);

        if (e.type == EventType.MouseDown && e.button == 0) {
          e.Use();
          HandleCellClick(_hoveredCell, e.shift);
        }
        else if (e.type == EventType.MouseDown && e.button == 1) {
          e.Use();
          RemoveSlotAtCell(_hoveredCell);
        }
      }

      HandleHotkeys(e);
      DrawPaintModeOverlay();
      sceneView.Repaint();
    }

    private void DrawCellHighlight(Vector2Int cell) {
      var cellSize = World.Grid.WorldGrid.cellSize;
      var cellMin = _builder.transform.position + new Vector3(cell.x * cellSize, 0.02f, cell.y * cellSize);
      var cellMax = cellMin + new Vector3(cellSize, 0, cellSize);
      var cellCenter = (cellMin + cellMax) * 0.5f;

      var existingSlot = _builder.GetSlotAtCell(cell.x, cell.y);
      var color = existingSlot != null ? HoverOccupiedColor : HoverColor;

      Handles.color = color;
      Handles.DrawSolidRectangleWithOutline(
        new[] { cellMin, new Vector3(cellMax.x, cellMin.y, cellMin.z), cellMax, new Vector3(cellMin.x, cellMin.y, cellMax.z) },
        color,
        Color.white
      );

      if (existingSlot != null) {
        Handles.Label(cellCenter + Vector3.up * 0.3f, $"[{existingSlot.type}]\nRight-click to remove", EditorStyles.whiteBoldLabel);
      }
      else {
        Handles.Label(cellCenter + Vector3.up * 0.3f, $"Click to place {_paintSlotType}", EditorStyles.whiteBoldLabel);
      }
    }

    private void HandleCellClick(Vector2Int cell, bool shiftHeld) {
      var existingSlot = _builder.GetSlotAtCell(cell.x, cell.y);

      if (shiftHeld && existingSlot != null) {
        RemoveSlotAtCell(cell);
      }
      else if (existingSlot == null) {
        AddSlotAtCell(cell);
      }
      else {
        CycleSlotType(existingSlot);
      }
    }

    private void AddSlotAtCell(Vector2Int cell) {
      Undo.RecordObject(_builder, "Add Slot");
      _builder.AddSlotAtCell(cell.x, cell.y, _paintSlotType);
      EditorUtility.SetDirty(_builder);
    }

    private void RemoveSlotAtCell(Vector2Int cell) {
      Undo.RecordObject(_builder, "Remove Slot");
      if (_builder.RemoveSlotAtCell(cell.x, cell.y)) {
        EditorUtility.SetDirty(_builder);
      }
    }

    private void CycleSlotType(SlotDefinition slot) {
      Undo.RecordObject(_builder, "Cycle Slot Type");
      var values = System.Enum.GetValues(typeof(SlotType));
      var index = System.Array.IndexOf(values, slot.type);
      slot.type = (SlotType)values.GetValue((index + 1) % values.Length);
      EditorUtility.SetDirty(_builder);
    }

    private void HandleHotkeys(Event e) {
      if (e.type != EventType.KeyDown) return;

      var handled = true;
      switch (e.keyCode) {
        case KeyCode.Alpha1: _paintSlotType = SlotType.Sleeping; break;
        case KeyCode.Alpha2: _paintSlotType = SlotType.Production; break;
        case KeyCode.Alpha3: _paintSlotType = SlotType.Storage; break;
        case KeyCode.Alpha4: _paintSlotType = SlotType.Utility; break;
        case KeyCode.Alpha5: _paintSlotType = SlotType.Entertainment; break;
        case KeyCode.Escape: _paintMode = false; break;
        default: handled = false; break;
      }

      if (handled) {
        e.Use();
        Repaint();
      }
    }

    private void DrawPaintModeOverlay() {
      Handles.BeginGUI();

      var rect = new Rect(10, 10, 220, 120);
      GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

      GUILayout.BeginArea(new Rect(15, 15, 210, 110));
      GUILayout.Label("🎨 Slot Paint Mode", EditorStyles.boldLabel);
      GUILayout.Space(5);
      GUILayout.Label($"Type: {_paintSlotType}", EditorStyles.label);
      GUILayout.Label("LMB: Place | RMB/Shift+LMB: Remove", EditorStyles.miniLabel);
      GUILayout.Label("1-5: Select type | Esc: Exit", EditorStyles.miniLabel);
      GUILayout.Space(5);
      if (GUILayout.Button("Exit Paint Mode", GUILayout.Height(20))) {
        _paintMode = false;
      }
      GUILayout.EndArea();

      Handles.EndGUI();
    }

    public override void OnInspectorGUI() {
      // Draw default Odin inspector
      base.OnInspectorGUI();

      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

      // Paint Mode toggle
      EditorGUILayout.BeginHorizontal();
      
      var paintModeStyle = new GUIStyle(GUI.skin.button);
      if (_paintMode) {
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        paintModeStyle.fontStyle = FontStyle.Bold;
      }
      
      if (GUILayout.Button(_paintMode ? "🎨 Paint Mode ON" : "🎨 Paint Mode", paintModeStyle, GUILayout.Height(28))) {
        _paintMode = !_paintMode;
        SceneView.RepaintAll();
      }
      GUI.backgroundColor = Color.white;
      
      _paintSlotType = (SlotType)EditorGUILayout.EnumPopup(_paintSlotType, GUILayout.Width(100), GUILayout.Height(28));
      
      EditorGUILayout.EndHorizontal();

      if (_paintMode) {
        EditorGUILayout.HelpBox("Click cells in Scene view to place slots.\nShift+Click or Right-Click to remove.\nKeys 1-5 to select type. Esc to exit.", MessageType.Info);
      }

      EditorGUILayout.Space(5);

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
      var cellSize = World.Grid.WorldGrid.cellSize;
      
      pos.x = Mathf.Round(pos.x / cellSize) * cellSize;
      pos.z = Mathf.Round(pos.z / cellSize) * cellSize;
      
      _builder.transform.position = pos;
      
      EditorUtility.SetDirty(_builder);
      Debug.Log($"[StructureFoundationBuilder] Snapped to grid: {pos}");
    }

    private void AddSlotAtCenter() {
      Undo.RecordObject(_builder, "Add Slot");
      
      var cellSize = World.Grid.WorldGrid.cellSize;
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

      // Create a temp copy that preserves nested prefab links where possible
      var tempGO = BuildTempCopyPreservingNestedPrefabs(_builder.gameObject, prefabName);
      if (tempGO == null) {
        EditorUtility.DisplayDialog("Error", "Failed to create temp copy for prefab.", "OK");
        return;
      }

      // Remove the builder component from temp copy
      var builderOnCopy = tempGO.GetComponent<StructureFoundationBuilder>();
      if (builderOnCopy != null) {
        Object.DestroyImmediate(builderOnCopy);
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
      Object.DestroyImmediate(tempGO);

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

    // Build a temporary copy of the original object but when a node has a prefab asset source,
    // instantiate that asset (preserving nested prefab links). For other nodes, clone components and recurse.
    private static GameObject BuildTempCopyPreservingNestedPrefabs(GameObject originalRoot, string tempName) {
      if (originalRoot == null) return null;

      var tempRoot = new GameObject(tempName);

      // Copy root-level components except Transform (will be handled by GameObject transform)
      // We'll keep the root mostly empty and only copy children since StructureFoundationBuilder is an editor helper.

      void CopyNode(Transform src, Transform destParent) {
        var srcGO = src.gameObject;
        var sourceAsset = PrefabUtility.GetCorrespondingObjectFromSource(srcGO);

        GameObject newGO;

        if (sourceAsset != null) {
          // If this node is a prefab instance, instantiate its asset to preserve prefab connection
          newGO = PrefabUtility.InstantiatePrefab(sourceAsset) as GameObject;
          if (newGO == null) {
            // fallback to cloning
            newGO = Object.Instantiate(srcGO);
          }

          newGO.name = srcGO.name;
          newGO.transform.SetParent(destParent, false);

          // Copy local transform
          newGO.transform.localPosition = src.localPosition;
          newGO.transform.localRotation = src.localRotation;
          newGO.transform.localScale = src.localScale;

          // Do NOT recurse into children — instantiated prefab carries its own hierarchy (and nested prefab links)
        }
        else {
          // Clone the GameObject but strip its children so we can process them individually
          newGO = Object.Instantiate(srcGO);
          newGO.name = srcGO.name;

          // Detach and hold clones of children created by Instantiate so we can replace them
          var detachedChildren = new List<GameObject>();
          for (var i = 0; i < newGO.transform.childCount; i++) {
            detachedChildren.Add(newGO.transform.GetChild(i).gameObject);
          }
          foreach (var c in detachedChildren) c.transform.SetParent(null, false);

          // Parent the cloned root under destination
          newGO.transform.SetParent(destParent, false);
          newGO.transform.localPosition = src.localPosition;
          newGO.transform.localRotation = src.localRotation;
          newGO.transform.localScale = src.localScale;

          // Now recursively copy children from original into the newly cloned GO
          foreach (Transform child in src) {
            CopyNode(child, newGO.transform);
          }

          // Cleanup detached clones (they are duplicates created by Instantiate)
          foreach (var c in detachedChildren) Object.DestroyImmediate(c);
        }
      }

      // Copy children of original root (we don't copy the original root GameObject itself)
      foreach (Transform child in originalRoot.transform) {
        CopyNode(child, tempRoot.transform);
      }

      // Copy transform of root to match original
      tempRoot.transform.position = originalRoot.transform.position;
      tempRoot.transform.rotation = originalRoot.transform.rotation;
      tempRoot.transform.localScale = originalRoot.transform.localScale;

      return tempRoot;
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
