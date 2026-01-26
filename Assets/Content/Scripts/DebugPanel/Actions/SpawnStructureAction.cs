using Content.Scripts.Building.Data;
using Content.Scripts.Building.Services;
using UnityEngine;

namespace Content.Scripts.DebugPanel.Actions {
  public class SpawnStructureAction : IDebugAction {
    private readonly StructuresModule _structuresModule;
    private readonly StructureDefinitionSO _definitionSo;

    public SpawnStructureAction(StructuresModule structuresModule, StructureDefinitionSO definitionSo, string displayName) {
      _structuresModule = structuresModule;
      _definitionSo = definitionSo;
      this.displayName = displayName;
    }

    public string displayName { get; }
    public DebugCategory category => DebugCategory.Structure;
    public DebugActionType actionType => DebugActionType.RequiresWorldPosition;
    
    // Expose footprint for grid visualization
    public Vector2Int footprint => _definitionSo != null ? _definitionSo.footprint : Vector2Int.one;

    public void Execute(DebugActionContext context) {
      var structure = _structuresModule.PlaceBlueprint(_definitionSo, context.worldPosition);
      if (structure != null) {
        Debug.Log($"[DebugAction] Structure {displayName} placed at {context.worldPosition}");
      } else {
        Debug.LogError($"[DebugAction] Failed to place structure {displayName}");
      }
    }
  }
}