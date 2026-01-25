using System.Collections.Generic;
using Content.Scripts.Building.Runtime;
using Content.Scripts.Descriptors.Tags;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif

namespace Content.Scripts.Building.Data {
  [CreateAssetMenu(fileName = "StructureDef", menuName = "Building/Structure Definition")]
  public class StructureDefinitionSO : SerializedScriptableObject {
    private const string StructurePartsGroup = "StructureParts";

    [Title("Identity")]
    [Tooltip("Unique structure identifier")]
    public string structureId;

    [Title("Footprint")]
    [Tooltip("Grid size in cells (X, Z)")]
    public Vector2Int footprint = new(3, 3);

    [Tooltip("Foundation prefab (must have StructureDescription component)")]
    [AssetsOnly]
    [ValueDropdown("GetAvailableFoundationPrefabs", IsUniqueList = true, DropdownTitle = "Select Foundation Prefab")]
    [ValidateInput("ValidateFoundationPrefab", "Prefab footprint doesn't match!")]
    [OnValueChanged("OnFoundationPrefabChanged")]
    public GameObject foundationPrefab;

    [Tooltip("Resources and work to build foundation")]
    [FoldoutGroup("Construction")]
    [InlineProperty, HideLabel]
    public ConstructionData constructionData = new();

    [Title("Slots")]
    [Tooltip("Available slots for modules")]
    [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
    public SlotDefinition[] slots;

    [Title("Core Module")]
    [Tooltip("Required module to make structure functional. Other modules blocked until core is built.")]
    public ModuleDefinitionSO coreModule;

    [Tooltip("Which slot(s) core module occupies. If empty, auto-finds first compatible slots.")]
    [ValueDropdown("GetSlotIds")]
    public string[] coreModuleSlotIds;

    [Title("Entry Points")]
    [Tooltip("Which sides allow entry (stairs placement)")]
    [EnumToggleButtons]
    public EntryDirection entryDirections = EntryDirection.All;

    [Tooltip("Stairs/ramp prefab for entry. Scaled by Y to match gap height.")]
    [AssetsOnly]
    public GameObject stairsPrefab;

    [Tooltip("NavMeshLink prefab. If null, link created programmatically.")]
    [AssetsOnly]
    public NavMeshLink navMeshLinkPrefab;

    [Title("Wall Configuration")]
    [Tooltip("Solid wall prefab (1 cell width)")]
    [AssetsOnly]
    [ValueDropdown("GetSolidWallPrefabs", IsUniqueList = true, DropdownTitle = "Select Solid Wall")]
    public GameObject solidWallPrefab;

    [Tooltip("Wall with doorway for entry points")]
    [AssetsOnly]
    [ValueDropdown("GetDoorwayWallPrefabs", IsUniqueList = true, DropdownTitle = "Select Doorway Wall")]
    public GameObject doorwayWallPrefab;

    [Tooltip("Passage/arch for expansion connections")]
    [AssetsOnly]
    [ValueDropdown("GetPassageWallPrefabs", IsUniqueList = true, DropdownTitle = "Select Passage Wall")]
    public GameObject passageWallPrefab;

    [Tooltip("Max height difference for stairs placement")]
    [MinValue(0.1f)]
    public float maxStairsHeight = 3f;

    [Tooltip("Prefab for procedural supports under foundation")]
    [AssetsOnly]
    public GameObject supportPrefab;

    // TODO Phase 9: Expansion system
    // public ExpansionDefinition[] expansions;

#if UNITY_EDITOR
    
    #region Foundation Prefab Dropdown
    
    private IEnumerable<ValueDropdownItem<GameObject>> GetAvailableFoundationPrefabs() {
      var result = new List<ValueDropdownItem<GameObject>>();
      result.Add(new ValueDropdownItem<GameObject>("None", null));

      var settings = AddressableAssetSettingsDefaultObject.Settings;
      if (settings == null) return result;

      foreach (var group in settings.groups) {
        if (group == null) continue;

        foreach (var entry in group.entries) {
          var path = AssetDatabase.GUIDToAssetPath(entry.guid);
          var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
          
          if (prefab == null) continue;
          
          var desc = prefab.GetComponent<StructureTag>();
          if (desc == null) continue;

          var footprintMatch = desc.footprint == footprint;
          var label = footprintMatch 
            ? $"{prefab.name} ({desc.footprint.x}x{desc.footprint.y})"
            : $"{prefab.name} ({desc.footprint.x}x{desc.footprint.y}) ⚠️";

          result.Add(new ValueDropdownItem<GameObject>(label, prefab));
        }
      }

      return result;
    }

    private bool ValidateFoundationPrefab(GameObject prefab) {
      if (prefab == null) return true;
      
      var desc = prefab.GetComponent<StructureTag>();
      if (desc == null) return false;
      
      return desc.footprint == footprint;
    }

    private void OnFoundationPrefabChanged() {
      if (foundationPrefab == null) return;
      
      var desc = foundationPrefab.GetComponent<StructureTag>();
      if (desc == null) return;

      if (desc.footprint == footprint && (slots == null || slots.Length == 0)) {
        CopySlotsFromPrefab();
      }
    }
    
    #endregion

    #region Wall Prefab Dropdowns

    private IEnumerable<ValueDropdownItem<GameObject>> GetSolidWallPrefabs() {
      return GetWallPrefabsByType(WallSegmentType.Solid);
    }

    private IEnumerable<ValueDropdownItem<GameObject>> GetDoorwayWallPrefabs() {
      return GetWallPrefabsByType(WallSegmentType.Doorway);
    }

    private IEnumerable<ValueDropdownItem<GameObject>> GetPassageWallPrefabs() {
      return GetWallPrefabsByType(WallSegmentType.Passage);
    }

    private IEnumerable<ValueDropdownItem<GameObject>> GetWallPrefabsByType(WallSegmentType targetType) {
      var result = new List<ValueDropdownItem<GameObject>>();
      result.Add(new ValueDropdownItem<GameObject>("None", null));

      var settings = AddressableAssetSettingsDefaultObject.Settings;
      if (settings == null) return result;

      var group = settings.FindGroup(StructurePartsGroup);
      if (group == null) return result;

      foreach (var entry in group.entries) {
        var path = AssetDatabase.GUIDToAssetPath(entry.guid);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        
        if (prefab == null) continue;
        
        var partDesc = prefab.GetComponent<StructurePartDescription>();
        if (partDesc == null) continue;
        if (partDesc.wallType != targetType) continue;

        var label = string.IsNullOrEmpty(partDesc.description)
          ? prefab.name
          : $"{prefab.name} - {partDesc.description}";

        result.Add(new ValueDropdownItem<GameObject>(label, prefab));
      }

      return result;
    }

    #endregion

    #region Buttons

    [Button("Validate", ButtonSizes.Small), PropertyOrder(-1)]
    private void Validate() {
      if (string.IsNullOrEmpty(structureId)) {
        Debug.LogError($"[{name}] structureId is empty!", this);
        return;
      }
      if (foundationPrefab == null) {
        Debug.LogError($"[{name}] foundationPrefab is not assigned!", this);
        return;
      }
      
      var desc = foundationPrefab.GetComponent<StructureTag>();
      if (desc == null) {
        Debug.LogError($"[{name}] foundationPrefab has no StructureDescription component!", this);
        return;
      }
      if (desc.footprint != footprint) {
        Debug.LogError($"[{name}] Footprint mismatch! Definition: {footprint}, Prefab: {desc.footprint}", this);
        return;
      }
      
      if (slots == null || slots.Length == 0) {
        Debug.LogWarning($"[{name}] No slots defined", this);
      }
      else {
        foreach (var slot in slots) {
          if (string.IsNullOrEmpty(slot.slotId)) {
            Debug.LogWarning($"[{name}] Slot has empty slotId", this);
          }
        }
      }
      
      // Core module validation
      if (coreModule != null) {
        if (coreModuleSlotIds != null && coreModuleSlotIds.Length > 0) {
          foreach (var slotId in coreModuleSlotIds) {
            var found = false;
            foreach (var slot in slots) {
              if (slot.slotId == slotId) { found = true; break; }
            }
            if (!found) {
              Debug.LogWarning($"[{name}] coreModuleSlotId '{slotId}' not found in slots", this);
            }
          }
        }
        var coreFootprintSize = coreModule.slotFootprint.x * coreModule.slotFootprint.y;
        if (coreModuleSlotIds != null && coreModuleSlotIds.Length > 0 && coreModuleSlotIds.Length != coreFootprintSize) {
          Debug.LogWarning($"[{name}] coreModuleSlotIds count ({coreModuleSlotIds.Length}) doesn't match coreModule footprint ({coreFootprintSize})", this);
        }
      }
      
      var hasNavMeshSurface = foundationPrefab.GetComponentInChildren<NavMeshSurface>(true) != null;
      if (!hasNavMeshSurface) {
        Debug.LogWarning($"[{name}] foundationPrefab has no NavMeshSurface in children!", this);
      }
      
      if (entryDirections != EntryDirection.None && stairsPrefab == null) {
        Debug.LogWarning($"[{name}] entryDirections set but no stairsPrefab assigned", this);
      }

      // Wall prefabs validation
      if (solidWallPrefab == null) {
        Debug.LogWarning($"[{name}] solidWallPrefab is not assigned", this);
      }
      if (doorwayWallPrefab == null) {
        Debug.LogWarning($"[{name}] doorwayWallPrefab is not assigned", this);
      }
      
      var coreInfo = coreModule != null ? $" (core: {coreModule.moduleId})" : "";
      Debug.Log($"[{name}] Valid ✓{coreInfo}", this);
    }
    
    private IEnumerable<string> GetSlotIds() {
      if (slots == null) yield break;
      foreach (var slot in slots) {
        if (!string.IsNullOrEmpty(slot.slotId)) yield return slot.slotId;
      }
    }

    [Button("Copy Slots from Prefab"), PropertyOrder(-1)]
    // [ShowIf("@foundationPrefab != null && foundationPrefab.GetComponent<Content.Scripts.Building.Runtime.StructureDescription>() != null")]
    private void CopySlotsFromPrefab() {
      var desc = foundationPrefab.GetComponent<StructureTag>();
      if (desc == null || desc.slots == null) return;

      footprint = desc.footprint;
      entryDirections = desc.entryDirections;
      
      slots = new SlotDefinition[desc.slots.Length];
      for (var i = 0; i < desc.slots.Length; i++) {
        var src = desc.slots[i];
        slots[i] = new SlotDefinition {
          slotId = src.slotId,
          type = src.type,
          localPosition = src.localPosition,
          localRotation = src.localRotation,
          acceptedModuleTags = src.acceptedModuleTags?.Clone() as string[],
          isInterior = src.isInterior,
          startsLocked = src.startsLocked
        };
      }

      Debug.Log($"[{name}] Copied {slots.Length} slots from prefab");
    }

    #endregion

    private void OnValidate() {
      if (string.IsNullOrEmpty(structureId)) {
        structureId = name;
      }
    }
#endif
  }
}
