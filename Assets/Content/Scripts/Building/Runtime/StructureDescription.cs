using Content.Scripts.Building.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Attached to foundation prefab to store structure metadata.
  /// Used for validation when assigning prefab to StructureDefinitionSO.
  /// </summary>
  public class StructureDescription : MonoBehaviour {
    [Title("Structure Info")]
    [ReadOnly]
    [Tooltip("Footprint size this structure was built for")]
    public Vector2Int footprint;

    // [ReadOnly]
    [Tooltip("Allowed entry directions")]
    public EntryDirection entryDirections;

    [ReadOnly]
    [Tooltip("Number of slots defined")]
    public int slotCount;

    // [ReadOnly]
    [Tooltip("Slot definitions snapshot")]
    // [TableList(IsReadOnly = true)]
    public SlotDefinition[] slots;

#if UNITY_EDITOR
    [Button("Log Info")]
    private void LogInfo() {
      Debug.Log($"[StructureDescription] {name}: {footprint.x}x{footprint.y}, {slotCount} slots, Entry: {entryDirections}");
    }
#endif
  }
}
