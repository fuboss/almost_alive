using Content.Scripts.Building.Data;
using Content.Scripts.Building.Runtime;
using Content.Scripts.Building.Services;
using UnityEngine;

namespace Content.Scripts.DebugPanel.Actions {
  /// <summary>
  /// Debug action to instantly place a module into a structure (no construction).
  /// </summary>
  public class PlaceModuleAction : IDebugAction {
    private readonly ModulePlacementService _modulePlacement;
    private readonly ModuleDefinitionSO _moduleDef;

    public PlaceModuleAction(ModulePlacementService modulePlacement, ModuleDefinitionSO moduleDef, string displayName) {
      _modulePlacement = modulePlacement;
      _moduleDef = moduleDef;
      this.displayName = displayName;
    }

    public string displayName { get; }
    public DebugCategory category => DebugCategory.Module;
    public DebugActionType actionType => DebugActionType.RequiresStructure;

    public void Execute(DebugActionContext context) {
      if (context.targetStructure == null) {
        Debug.LogError("[PlaceModuleAction] No structure selected");
        return;
      }

      var structure = context.targetStructure;
      var isCoreModule = structure.definition?.coreModule == _moduleDef;
      
      if (!isCoreModule && structure.requiresCore && !structure.isCoreBuilt) {
        Debug.LogWarning($"[PlaceModuleAction] Structure requires core module first. Core: {structure.definition.coreModule?.moduleId}");
        return;
      }

      // Use instant placement for debug
      var module = _modulePlacement.InstantPlaceModule(structure, _moduleDef);
      if (module != null) {
        var coreTag = isCoreModule ? " [CORE]" : "";
        Debug.Log($"[PlaceModuleAction] Instantly placed {_moduleDef.moduleId}{coreTag} in {structure.name}");
      }
      else {
        Debug.LogWarning($"[PlaceModuleAction] Failed to place {_moduleDef.moduleId} - no suitable slots or clearance blocked");
      }
    }
  }

  /// <summary>
  /// Debug action to assign a module for construction (creates UnfinishedModuleActor).
  /// </summary>
  public class AssignModuleAction : IDebugAction {
    private readonly ModulePlacementService _modulePlacement;
    private readonly ModuleDefinitionSO _moduleDef;

    public AssignModuleAction(ModulePlacementService modulePlacement, ModuleDefinitionSO moduleDef, string displayName) {
      _modulePlacement = modulePlacement;
      _moduleDef = moduleDef;
      this.displayName = displayName;
    }

    public string displayName { get; }
    public DebugCategory category => DebugCategory.Module;
    public DebugActionType actionType => DebugActionType.RequiresStructure;

    public void Execute(DebugActionContext context) {
      if (context.targetStructure == null) {
        Debug.LogError("[AssignModuleAction] No structure selected");
        return;
      }

      var structure = context.targetStructure;
      var isCoreModule = structure.definition?.coreModule == _moduleDef;
      
      if (!isCoreModule && structure.requiresCore && !structure.isCoreBuilt) {
        Debug.LogWarning($"[AssignModuleAction] Structure requires core module first. Core: {structure.definition.coreModule?.moduleId}");
        return;
      }

      var unfinished = _modulePlacement.AssignModule(structure, _moduleDef);
      if (unfinished != null) {
        var coreTag = isCoreModule ? " [CORE]" : "";
        Debug.Log($"[AssignModuleAction] Assigned {_moduleDef.moduleId}{coreTag} for construction in {structure.name}");
      }
      else {
        Debug.LogWarning($"[AssignModuleAction] Failed to assign {_moduleDef.moduleId} - no suitable slots or clearance blocked");
      }
    }
  }
}
