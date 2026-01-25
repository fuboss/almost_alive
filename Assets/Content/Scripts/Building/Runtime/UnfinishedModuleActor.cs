using System.Collections.Generic;
using Content.Scripts.AI.Craft;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.Services;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Unfinished module being constructed in a structure slot.
  /// </summary>
  public class UnfinishedModuleActor : UnfinishedActorBase {
    [Title("Module")]
    [ShowInInspector, ReadOnly] private ModuleDefinitionSO _moduleDef;
    [ShowInInspector, ReadOnly] private Structure _targetStructure;
    [ShowInInspector, ReadOnly] private Slot _anchorSlot;

    [Inject] private ModulePlacementService _modulePlacement;

    public ModuleDefinitionSO moduleDef => _moduleDef;
    public Structure targetStructure => _targetStructure;
    public Slot anchorSlot => _anchorSlot;

    public override float workRequired => _moduleDef?.recipe.workRequired ?? 100f;
    protected override IReadOnlyList<RecipeRequiredResource> requiredResources 
      => _moduleDef?.recipe.requiredResources;

    public void Initialize(ModuleDefinitionSO moduleDef, Structure structure, Slot anchorSlot) {
      _moduleDef = moduleDef;
      _targetStructure = structure;
      _anchorSlot = anchorSlot;
      _workProgress = 0f;
    }

    public override ActorDescription TryComplete() {
      if (!isReadyToComplete) {
        Debug.LogWarning($"[UnfinishedModule] Cannot complete - resources: {hasAllResources}, work: {workComplete}");
        return null;
      }

      if (_modulePlacement == null) {
        Debug.LogError("[UnfinishedModule] ModulePlacementService not injected!");
        return null;
      }

      if (_targetStructure == null || _anchorSlot == null) {
        Debug.LogError("[UnfinishedModule] Target structure or anchor slot is null!");
        return null;
      }

      // Complete the module via service
      var module = _modulePlacement.CompleteModule(this);
      if (module == null) {
        Debug.LogError($"[UnfinishedModule] Failed to complete {_moduleDef.moduleId}");
        return null;
      }

      Debug.Log($"[UnfinishedModule] Completed {_moduleDef.moduleId}!");
      
      // Return the module's actor description if it has one
      var actorDesc = module.GetComponent<ActorDescription>();
      
      Destroy(gameObject);
      return actorDesc;
    }

    private void OnDestroy() {
      // If destroyed without completion, clear the slot assignment
      if (_anchorSlot != null && _anchorSlot.isAssigned && _anchorSlot.GetAssignedModuleDef() == _moduleDef) {
        _targetStructure?.ClearModule(_anchorSlot);
      }
    }
  }
}
