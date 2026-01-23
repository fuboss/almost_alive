using System.Collections.Generic;
using Content.Scripts.AI;
using Content.Scripts.AI.Craft;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Data {
  [CreateAssetMenu(fileName = "ModuleDef", menuName = "Building/Module Definition")]
  public class ModuleDefinitionSO : ScriptableObject {
    [Tooltip("Unique module identifier")]
    public string moduleId;

    [Tooltip("Tags for matching with slot acceptedModuleTags")]
    [ValueDropdown("Tags")]
    public string[] tags;

    [Tooltip("Which slot types can hold this module")]
    public SlotType[] compatibleSlotTypes;

    [Tooltip("Resources and time required to build")]
    [FoldoutGroup("Recipe")]
    [InlineProperty, HideLabel]
    public RecipeData recipe = new();

    [Tooltip("Prefab to instantiate when built")]
    [AssetsOnly]
    public GameObject prefab;

    [Tooltip("Fraction of resources returned on deconstruct (0-1)")]
    [Range(0f, 1f)]
    public float deconstructReturnPercent = 0.5f;

#if UNITY_EDITOR
    private IEnumerable<string> Tags() => Tag.ALL_TAGS;

    [Button("Validate", ButtonSizes.Small), PropertyOrder(-1)]
    private void Validate() {
      if (string.IsNullOrEmpty(moduleId)) {
        Debug.LogError($"[{name}] moduleId is empty!", this);
        return;
      }
      if (prefab == null) {
        Debug.LogError($"[{name}] prefab is not assigned!", this);
        return;
      }
      if (compatibleSlotTypes == null || compatibleSlotTypes.Length == 0) {
        Debug.LogWarning($"[{name}] No compatible slot types defined", this);
      }
      Debug.Log($"[{name}] Valid ✓", this);
    }

    private void OnValidate() {
      if (string.IsNullOrEmpty(moduleId)) {
        moduleId = name;
      }
    }
#endif
  }
}
