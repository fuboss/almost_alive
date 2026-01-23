using Content.Scripts.Building.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Attached to wall/structure part prefabs to identify their type.
  /// Used for validation when assigning prefabs to StructureDefinitionSO.
  /// </summary>
  public class StructurePartDescription : MonoBehaviour {
    [Title("Part Info")]
    [Tooltip("What type of wall segment this prefab represents")]
    public WallSegmentType wallType;

    [Tooltip("Optional description")]
    [TextArea(1, 3)]
    public string description;

#if UNITY_EDITOR
    [Button("Log Info")]
    private void LogInfo() {
      Debug.Log($"[StructurePartDescription] {name}: {wallType}");
    }
#endif
  }
}
