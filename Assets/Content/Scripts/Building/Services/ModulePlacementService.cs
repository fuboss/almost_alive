using Content.Scripts.AI.GOAP;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.Runtime;
using Content.Scripts.Game;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Building.Services {
  /// <summary>
  /// Handles module placement and removal in structures.
  /// </summary>
  public class ModulePlacementService {
    [Inject] private ActorCreationModule _actorCreation;
    [Inject] private IObjectResolver _resolver;

    #region Assignment (creates UnfinishedModuleActor)

    /// <summary>
    /// Assign a module to structure slots and create UnfinishedModuleActor.
    /// Returns the unfinished actor, or null if failed.
    /// </summary>
    public UnfinishedModuleActor AssignModule(Structure structure, ModuleDefinitionSO moduleDef, 
      SlotPriority priority = SlotPriority.NORMAL) {
      
      if (structure == null || moduleDef == null) {
        Debug.LogError("[ModulePlacement] Structure or ModuleDefinition is null");
        return null;
      }

      var isCoreModule = IsCoreModule(structure, moduleDef);
      var anchorSlot = structure.AssignModuleToSlots(moduleDef, null, priority, isCoreModule);
      
      if (anchorSlot == null) {
        Debug.LogWarning($"[ModulePlacement] No suitable slots for {moduleDef.moduleId} in {structure.name}");
        return null;
      }

      return CreateUnfinishedModule(structure, anchorSlot, moduleDef);
    }

    /// <summary>
    /// Assign a module at specific anchor slot.
    /// </summary>
    public UnfinishedModuleActor AssignModuleAt(Structure structure, Slot anchorSlot, 
      ModuleDefinitionSO moduleDef, SlotPriority priority = SlotPriority.NORMAL) {
      
      if (structure == null || anchorSlot == null || moduleDef == null) {
        Debug.LogError("[ModulePlacement] Invalid parameters");
        return null;
      }

      var isCoreModule = IsCoreModule(structure, moduleDef);
      
      if (!structure.CanPlaceModule(moduleDef, anchorSlot, isCoreModule)) {
        Debug.LogWarning($"[ModulePlacement] Cannot place {moduleDef.moduleId} at {anchorSlot.slotId}");
        return null;
      }

      var actualAnchor = structure.AssignModuleToSlots(moduleDef, anchorSlot, priority, isCoreModule);
      if (actualAnchor == null) return null;

      return CreateUnfinishedModule(structure, actualAnchor, moduleDef);
    }

    /// <summary>
    /// Assign the core module for a structure.
    /// </summary>
    public UnfinishedModuleActor AssignCoreModule(Structure structure) {
      if (structure == null) return null;
      
      var def = structure.definition;
      if (def?.coreModule == null) {
        Debug.LogWarning("[ModulePlacement] Structure has no core module defined");
        return null;
      }

      if (structure.isCoreBuilt) {
        Debug.LogWarning("[ModulePlacement] Core module already built");
        return null;
      }

      Slot anchorSlot = null;
      if (def.coreModuleSlotIds != null && def.coreModuleSlotIds.Length > 0) {
        anchorSlot = structure.GetSlot(def.coreModuleSlotIds[0]);
      }

      return anchorSlot != null 
        ? AssignModuleAt(structure, anchorSlot, def.coreModule) 
        : AssignModule(structure, def.coreModule);
    }

    private UnfinishedModuleActor CreateUnfinishedModule(Structure structure, Slot anchorSlot, ModuleDefinitionSO moduleDef) {
      var worldPos = structure.transform.TransformPoint(anchorSlot.localPosition);
      
      if (!_actorCreation.TrySpawnActor("unfinished_module", worldPos, out var actorDesc)) {
        Debug.LogError("[ModulePlacement] Failed to spawn unfinished_module actor");
        structure.ClearModule(anchorSlot);
        return null;
      }

      var unfinished = actorDesc.GetComponent<UnfinishedModuleActor>();
      if (unfinished == null) {
        Debug.LogError("[ModulePlacement] unfinished_module prefab missing UnfinishedModuleActor component");
        Object.Destroy(actorDesc.gameObject);
        structure.ClearModule(anchorSlot);
        return null;
      }

      // Inject dependencies
      _resolver.Inject(unfinished);
      
      unfinished.transform.SetParent(structure.transform);
      unfinished.Initialize(moduleDef, structure, anchorSlot);

      Debug.Log($"[ModulePlacement] Created UnfinishedModule for {moduleDef.moduleId} at {anchorSlot.slotId}");
      return unfinished;
    }

    #endregion

    #region Completion (called by UnfinishedModuleActor)

    /// <summary>
    /// Complete module construction. Called by UnfinishedModuleActor.TryComplete().
    /// </summary>
    public Module CompleteModule(UnfinishedModuleActor unfinished) {
      if (unfinished == null) return null;

      var moduleDef = unfinished.moduleDef;
      var structure = unfinished.targetStructure;
      var anchorSlot = unfinished.anchorSlot;

      if (moduleDef == null || structure == null || anchorSlot == null) {
        Debug.LogError("[ModulePlacement] CompleteModule: invalid unfinished state");
        return null;
      }

      var isCoreModule = IsCoreModule(structure, moduleDef);
      return SpawnModule(structure, anchorSlot, moduleDef, isCoreModule);
    }

    #endregion

    #region Instant Placement (for debug/cheats)

    /// <summary>
    /// Instantly place a module without construction. For debug/cheats only.
    /// </summary>
    public Module InstantPlaceModule(Structure structure, ModuleDefinitionSO moduleDef, 
      SlotPriority priority = SlotPriority.NORMAL) {
      
      if (structure == null || moduleDef == null) {
        Debug.LogError("[ModulePlacement] Structure or ModuleDefinition is null");
        return null;
      }

      var isCoreModule = IsCoreModule(structure, moduleDef);
      var anchorSlot = structure.AssignModuleToSlots(moduleDef, null, priority, isCoreModule);
      
      if (anchorSlot == null) {
        Debug.LogWarning($"[ModulePlacement] No suitable slots for {moduleDef.moduleId} in {structure.name}");
        return null;
      }

      return SpawnModule(structure, anchorSlot, moduleDef, isCoreModule);
    }

    /// <summary>
    /// Instantly place a module at specific slot. For debug/cheats only.
    /// </summary>
    public Module InstantPlaceModuleAt(Structure structure, Slot anchorSlot, 
      ModuleDefinitionSO moduleDef, SlotPriority priority = SlotPriority.NORMAL) {
      
      if (structure == null || anchorSlot == null || moduleDef == null) {
        Debug.LogError("[ModulePlacement] Invalid parameters");
        return null;
      }

      var isCoreModule = IsCoreModule(structure, moduleDef);
      
      if (!structure.CanPlaceModule(moduleDef, anchorSlot, isCoreModule)) {
        Debug.LogWarning($"[ModulePlacement] Cannot place {moduleDef.moduleId} at {anchorSlot.slotId}");
        return null;
      }

      var actualAnchor = structure.AssignModuleToSlots(moduleDef, anchorSlot, priority, isCoreModule);
      if (actualAnchor == null) return null;

      return SpawnModule(structure, actualAnchor, moduleDef, isCoreModule);
    }

    /// <summary>
    /// Instantly place core module. For debug/cheats only.
    /// </summary>
    public Module InstantPlaceCoreModule(Structure structure) {
      if (structure == null) return null;
      
      var def = structure.definition;
      if (def?.coreModule == null) {
        Debug.LogWarning("[ModulePlacement] Structure has no core module defined");
        return null;
      }

      if (structure.isCoreBuilt) {
        Debug.LogWarning("[ModulePlacement] Core module already built");
        return null;
      }

      return InstantPlaceModule(structure, def.coreModule);
    }

    #endregion

    #region Removal

    /// <summary>
    /// Remove a module from structure, freeing all its slots.
    /// </summary>
    public void RemoveModule(Structure structure, Module module) {
      if (structure == null || module == null) return;

      var slots = structure.GetSlotsForModule(module);
      if (slots.Count == 0) {
        Debug.LogWarning($"[ModulePlacement] Module {module.name} not found in structure slots");
        Object.Destroy(module.gameObject);
        return;
      }

      var wasCoreModule = module.definition == structure.definition?.coreModule;
      
      Slot anchor = null;
      foreach (var slot in slots) {
        if (slot.builtModule == module) {
          anchor = slot;
          break;
        }
      }

      if (anchor != null) {
        structure.ClearModule(anchor);
      }
      else {
        Object.Destroy(module.gameObject);
      }

      if (wasCoreModule) {
        structure.SetCoreBuilt(false);
      }

      Debug.Log($"[ModulePlacement] Removed {module.definition?.moduleId ?? "unknown"} from {structure.name}");
    }

    #endregion

    #region Queries

    /// <summary>
    /// Get all modules placed in a structure.
    /// </summary>
    public Module[] GetModules(Structure structure) {
      if (structure == null) return System.Array.Empty<Module>();

      var modules = new System.Collections.Generic.List<Module>();
      foreach (var slot in structure.slots) {
        if (slot.isBuilt && slot.builtModule != null && !modules.Contains(slot.builtModule)) {
          modules.Add(slot.builtModule);
        }
      }

      return modules.ToArray();
    }

    /// <summary>
    /// Check if structure has space for a module.
    /// </summary>
    public bool HasSpaceFor(Structure structure, ModuleDefinitionSO moduleDef) {
      if (structure == null || moduleDef == null) return false;
      var isCoreModule = IsCoreModule(structure, moduleDef);
      return structure.FindSlotsForModule(moduleDef, isCoreModule) != null;
    }

    /// <summary>
    /// Check if structure can accept any non-core modules.
    /// </summary>
    public bool CanAcceptModules(Structure structure) {
      if (structure == null) return false;
      return !structure.requiresCore || structure.isCoreBuilt;
    }

    #endregion

    #region Helpers

    private bool IsCoreModule(Structure structure, ModuleDefinitionSO moduleDef) {
      return structure.definition?.coreModule == moduleDef;
    }

    private Module SpawnModule(Structure structure, Slot anchorSlot, ModuleDefinitionSO moduleDef, bool isCoreModule) {
      var actorKey = moduleDef.resultActorKey;
      if (string.IsNullOrEmpty(actorKey)) {
        Debug.LogError($"[ModulePlacement] ModuleDefinition {moduleDef.moduleId} has no resultActorKey!");
        return null;
      }

      var worldPos = structure.transform.TransformPoint(anchorSlot.localPosition);
      var worldRot = structure.transform.rotation * anchorSlot.localRotation;

      if (!_actorCreation.TrySpawnActor(actorKey, worldPos, out var actorDesc)) {
        Debug.LogError($"[ModulePlacement] Failed to spawn actor {actorKey}");
        return null;
      }

      var actorGO = actorDesc.gameObject;
      actorGO.transform.SetParent(structure.transform);
      actorGO.transform.rotation = worldRot;
      actorGO.name = $"Module_{moduleDef.moduleId}";

      var module = actorGO.GetComponent<Module>();
      if (module == null) {
        module = actorGO.AddComponent<Module>();
      }

      module.Initialize(moduleDef);
      anchorSlot.SetBuilt(module);

      if (isCoreModule) {
        structure.SetCoreBuilt(true);
        Debug.Log($"[ModulePlacement] Core module {moduleDef.moduleId} built!");
      }

      Debug.Log($"[ModulePlacement] Spawned {moduleDef.moduleId} at {anchorSlot.slotId}");
      return module;
    }

    #endregion
  }
}
