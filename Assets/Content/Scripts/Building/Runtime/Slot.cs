using Content.Scripts.Building.Data;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Runtime slot state.
  /// </summary>
  public enum SlotState {
    Empty,      // no module assigned
    Assigned,   // module assigned, awaiting construction
    Built       // module constructed
  }

  /// <summary>
  /// Runtime slot priority for construction.
  /// </summary>
  public enum SlotPriority {
    Low,
    Normal,
    High,
    Critical
  }

  /// <summary>
  /// Runtime representation of a module slot within a structure.
  /// </summary>
  public class Slot {
    public SlotDefinition definition;
    public SlotState state;
    public SlotPriority priority;
    public ModuleDefinitionSO assignedModuleDef;
    public Module builtModule;
    public object owner;  // IGoapAgent, nullable

    public Slot(SlotDefinition definition) {
      this.definition = definition;
      this.state = SlotState.Empty;
      this.priority = SlotPriority.Normal;
      this.assignedModuleDef = null;
      this.builtModule = null;
      this.owner = null;
    }

    public string slotId => definition.slotId;
    public SlotType type => definition.type;
    public Vector3 localPosition => definition.localPosition;
    public Quaternion localRotation => definition.localRotation;
    public bool isInterior => definition.isInterior;
    public bool isLocked => definition.startsLocked;  // TODO: runtime unlock

    public bool isEmpty => state == SlotState.Empty;
    public bool isAssigned => state == SlotState.Assigned;
    public bool isBuilt => state == SlotState.Built;

    /// <summary>
    /// Assign a module to this slot for construction.
    /// </summary>
    public bool AssignModule(ModuleDefinitionSO moduleDef, SlotPriority priority = SlotPriority.Normal) {
      if (state != SlotState.Empty) return false;
      if (moduleDef == null) return false;
      
      // Check compatibility
      if (!IsModuleCompatible(moduleDef)) return false;

      assignedModuleDef = moduleDef;
      this.priority = priority;
      state = SlotState.Assigned;
      return true;
    }

    /// <summary>
    /// Mark module as built.
    /// </summary>
    public void SetBuilt(Module module) {
      builtModule = module;
      state = SlotState.Built;
    }

    /// <summary>
    /// Clear the slot (deconstruct).
    /// </summary>
    public void Clear() {
      if (builtModule != null) {
        Object.Destroy(builtModule.gameObject);
        builtModule = null;
      }
      assignedModuleDef = null;
      state = SlotState.Empty;
      priority = SlotPriority.Normal;
    }

    /// <summary>
    /// Check if module is compatible with this slot.
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
      if (definition.acceptedModuleTags != null && definition.acceptedModuleTags.Length > 0) {
        if (moduleDef.tags == null || moduleDef.tags.Length == 0) return false;
        
        var hasMatchingTag = false;
        foreach (var moduleTag in moduleDef.tags) {
          foreach (var acceptedTag in definition.acceptedModuleTags) {
            if (moduleTag == acceptedTag) {
              hasMatchingTag = true;
              break;
            }
          }
          if (hasMatchingTag) break;
        }
        if (!hasMatchingTag) return false;
      }

      return true;
    }
  }
}
