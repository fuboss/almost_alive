using System.Linq;
using Content.Scripts.Building.Data;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Runtime slot state.
  /// </summary>
  public enum SlotState {
    EMPTY,
    ASSIGNED,  // anchor slot: module assigned, awaiting construction
    OCCUPIED,  // non-anchor slot: part of multi-slot module
    BUILT      // module constructed
  }

  /// <summary>
  /// Runtime slot priority for construction.
  /// </summary>
  public enum SlotPriority {
    LOW,
    NORMAL,
    HIGH,
    CRITICAL
  }

  /// <summary>
  /// Runtime representation of a module slot within a structure.
  /// Supports multi-slot modules via anchor/occupied pattern.
  /// </summary>
  public class Slot {
    public SlotDefinition definition;
    public SlotState state;
    public SlotPriority priority;
    
    // Anchor slot data (this slot owns the module)
    public ModuleDefinitionSO assignedModuleDef;
    public Module builtModule;
    
    // Multi-slot support: non-anchor slots reference their anchor
    public Slot anchorSlot;
    
    public object owner; // IGoapAgent, nullable

    public Slot(SlotDefinition definition) {
      this.definition = definition;
      this.state = SlotState.EMPTY;
      this.priority = SlotPriority.NORMAL;
      this.assignedModuleDef = null;
      this.builtModule = null;
      this.anchorSlot = null;
      this.owner = null;
    }

    #region Properties

    public string slotId => definition.slotId;
    public SlotType type => definition.type;
    public Vector3 localPosition => definition.localPosition;
    public Quaternion localRotation => definition.localRotation;
    public bool isInterior => definition.isInterior;
    public bool isLocked => definition.startsLocked; // TODO: runtime unlock

    public bool isEmpty => state == SlotState.EMPTY;
    public bool isAssigned => state == SlotState.ASSIGNED;
    public bool isOccupied => state == SlotState.OCCUPIED;
    public bool isBuilt => state == SlotState.BUILT;
    
    /// <summary>True if this slot is the anchor (owner) of a multi-slot module.</summary>
    public bool isAnchor => anchorSlot == null && (isAssigned || isBuilt);
    
    /// <summary>True if slot is used by any module (anchor or occupied).</summary>
    public bool isInUse => state != SlotState.EMPTY;
    
    /// <summary>Get the module occupying this slot (from anchor if occupied).</summary>
    public Module GetModule() {
      if (isBuilt) return builtModule;
      if (isOccupied && anchorSlot != null) return anchorSlot.builtModule;
      return null;
    }
    
    /// <summary>Get assigned module def (from anchor if occupied).</summary>
    public ModuleDefinitionSO GetAssignedModuleDef() {
      if (isAssigned) return assignedModuleDef;
      if (isOccupied && anchorSlot != null) return anchorSlot.assignedModuleDef;
      return null;
    }

    #endregion

    #region Assignment (single slot - legacy support)

    /// <summary>
    /// Assign a module to this slot for construction (single-slot module).
    /// For multi-slot modules use Structure.AssignModuleToSlots().
    /// </summary>
    public bool AssignModule(ModuleDefinitionSO moduleDef, SlotPriority priority = SlotPriority.NORMAL) {
      if (state != SlotState.EMPTY) return false;
      if (moduleDef == null) return false;
      if (!IsModuleCompatible(moduleDef)) return false;

      // Single-slot module check
      if (moduleDef.slotFootprint.x > 1 || moduleDef.slotFootprint.y > 1) {
        Debug.LogWarning($"[Slot] Cannot assign multi-slot module {moduleDef.moduleId} to single slot. Use Structure.AssignModuleToSlots()");
        return false;
      }

      assignedModuleDef = moduleDef;
      this.priority = priority;
      state = SlotState.ASSIGNED;
      anchorSlot = null; // this is anchor
      return true;
    }

    #endregion

    #region Multi-Slot Assignment (called by Structure)

    /// <summary>
    /// Mark this slot as anchor for a multi-slot module.
    /// Called by Structure.AssignModuleToSlots().
    /// </summary>
    internal void AssignAsAnchor(ModuleDefinitionSO moduleDef, SlotPriority priority) {
      assignedModuleDef = moduleDef;
      this.priority = priority;
      state = SlotState.ASSIGNED;
      anchorSlot = null;
    }

    /// <summary>
    /// Mark this slot as occupied by a multi-slot module (not anchor).
    /// Called by Structure.AssignModuleToSlots().
    /// </summary>
    internal void AssignAsOccupied(Slot anchor) {
      anchorSlot = anchor;
      state = SlotState.OCCUPIED;
      assignedModuleDef = null; // data lives in anchor
    }

    #endregion

    #region Build & Clear

    /// <summary>
    /// Mark module as built (anchor slot only).
    /// </summary>
    public void SetBuilt(Module module) {
      builtModule = module;
      state = SlotState.BUILT;
    }

    /// <summary>
    /// Clear the slot. For multi-slot modules, must be called on anchor first.
    /// </summary>
    public void Clear() {
      if (builtModule != null) {
        Object.Destroy(builtModule.gameObject);
        builtModule = null;
      }

      assignedModuleDef = null;
      anchorSlot = null;
      state = SlotState.EMPTY;
      priority = SlotPriority.NORMAL;
    }
    
    /// <summary>
    /// Clear occupied slot (non-anchor). Called by Structure when clearing module.
    /// </summary>
    internal void ClearOccupied() {
      if (state != SlotState.OCCUPIED) return;
      anchorSlot = null;
      state = SlotState.EMPTY;
    }

    #endregion

    #region Compatibility

    /// <summary>
    /// Check if module is compatible with this slot (type + tags).
    /// </summary>
    public bool IsModuleCompatible(ModuleDefinitionSO moduleDef) {
      if (moduleDef == null) return false;

      // Check slot type compatibility
      if (moduleDef.compatibleSlotTypes != null && moduleDef.compatibleSlotTypes.Length > 0) {
        var compatible = false;
        foreach (var slotType in moduleDef.compatibleSlotTypes) {
          if (slotType == type) {
            compatible = true;
            break;
          }
        }
        if (!compatible) return false;
      }

      // Check tag compatibility (if slot has accepted tags)
      if (definition.acceptedModuleTags is not { Length: > 0 }) return true;
      if (moduleDef.tags == null || moduleDef.tags.Length == 0) return false;

      return IsAcceptingTags(moduleDef.tags);
    }

    public bool IsAcceptingTags(string[] moduleActorTags) {
      return moduleActorTags
        .Any(moduleTag => definition.acceptedModuleTags
          .Any(acceptedTag => moduleTag == acceptedTag)
        );
    }

    #endregion
  }
}
