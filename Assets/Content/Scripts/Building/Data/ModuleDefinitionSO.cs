using System.Collections.Generic;
using Content.Scripts.AI;
using Content.Scripts.AI.Craft;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Data {
  [CreateAssetMenu(fileName = "ModuleDef", menuName = "Building/Module Definition")]
  public class ModuleDefinitionSO : ScriptableObject {
    [Title("Identity")]
    [Tooltip("Unique module identifier")]
    public string moduleId;

    [Tooltip("Optional tags for fine-grained slot filtering. If slot has acceptedModuleTags, module must have matching tag.")]
    [ValueDropdown("Tags")]
    public string[] tags;

    [Title("Placement")]
    [Tooltip("Which slot types can hold this module (all occupied slots must match)")]
    public SlotType[] compatibleSlotTypes;

    [Tooltip("How many slots this module occupies (X × Y). (1,1) = single slot.")]
    [MinValue(1)]
    public Vector2Int slotFootprint = Vector2Int.one;

    [Tooltip("Required empty slots around module for agent pathfinding. 0 = no clearance, 1 = 1-slot ring.")]
    [MinValue(0)]
    public int clearanceRadius;

    [Title("Construction")]
    [Tooltip("Resources and work required to build. ResultActorKey defines spawned actor.")]
    [InlineProperty, HideLabel]
    public RecipeData recipe = new();

    [Tooltip("Fraction of resources returned on deconstruct (0-1)")]
    [Range(0f, 1f)]
    public float deconstructReturnPercent = 0.5f;

    /// <summary>Actor key for spawning via ActorCreationModule.</summary>
    public string resultActorKey => recipe.resultActorKey;

#if UNITY_EDITOR
    private IEnumerable<string> Tags() => Tag.ALL_TAGS;

    [Button("Validate", ButtonSizes.Small), PropertyOrder(-1)]
    private void Validate() {
      if (string.IsNullOrEmpty(moduleId)) {
        Debug.LogError($"[{name}] moduleId is empty!", this);
        return;
      }
      if (string.IsNullOrEmpty(recipe.resultActorKey)) {
        Debug.LogError($"[{name}] recipe.resultActorKey is empty!", this);
        return;
      }
      if (compatibleSlotTypes == null || compatibleSlotTypes.Length == 0) {
        Debug.LogWarning($"[{name}] No compatible slot types defined", this);
      }
      if (slotFootprint.x < 1 || slotFootprint.y < 1) {
        Debug.LogError($"[{name}] slotFootprint must be at least (1,1)!", this);
        return;
      }
      
      var clearanceInfo = clearanceRadius > 0 ? $", clearance: {clearanceRadius}" : "";
      Debug.Log($"[{name}] Valid ✓ (footprint: {slotFootprint.x}x{slotFootprint.y}{clearanceInfo})", this);
    }

    private void OnValidate() {
      if (string.IsNullOrEmpty(moduleId)) {
        moduleId = name;
      }
    }
#endif
  }
}
