using Content.Scripts.AI.Craft;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Data {
  [CreateAssetMenu(fileName = "StructureDef", menuName = "Building/Structure Definition")]
  public class StructureDefinitionSO : ScriptableObject {
    [Tooltip("Unique structure identifier")]
    public string structureId;

    [Tooltip("Grid size in cells (X, Z)")]
    public Vector2Int footprint = new(3, 3);

    [Tooltip("Foundation prefab (walls, roof, core)")]
    [AssetsOnly]
    public GameObject foundationPrefab;

    [Tooltip("Resources and time to build foundation")]
    [FoldoutGroup("Foundation Recipe")]
    [InlineProperty, HideLabel]
    public RecipeData foundationRecipe = new();

    [Tooltip("Available slots for modules")]
    [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
    public SlotDefinition[] slots;

    // TODO Phase 9: Expansion system
    // public ExpansionDefinition[] expansions;

#if UNITY_EDITOR
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
      Debug.Log($"[{name}] Valid ✓", this);
    }

    private void OnValidate() {
      if (string.IsNullOrEmpty(structureId)) {
        structureId = name;
      }
    }
#endif
  }
}
