using System.Collections.Generic;
using Content.Scripts.Building.Data;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  public enum StructureState {
    BLUEPRINT,
    UNDER_CONSTRUCTION,
    BUILT,
    DAMAGED,
    DESTROYED
  }

  /// <summary>
  /// Runtime structure instance in the world.
  /// Data-only — all logic handled by services.
  /// </summary>
  public class Structure : MonoBehaviour {
    [Title("Runtime State")]
    [ShowInInspector, ReadOnly] private StructureDefinitionSO _definition;
    [ShowInInspector, ReadOnly] private StructureState _state = StructureState.BLUEPRINT;
    [ShowInInspector, ReadOnly] private float _hp;
    [ShowInInspector, ReadOnly] private float _maxHp = 100f;
    [ShowInInspector, ReadOnly] private bool _isCoreBuilt;

    [Title("Runtime Data")]
    [ShowInInspector, ReadOnly] private readonly List<Slot> _slots = new();
    [ShowInInspector, ReadOnly] private readonly List<WallSegment> _wallSegments = new();
    [ShowInInspector, ReadOnly] private readonly List<EntryPoint> _entryPoints = new();
    [ShowInInspector, ReadOnly] private readonly List<GameObject> _supports = new();
    
    private NavMeshSurface _floorNavMeshSurface;
    private float _cellSize;

    #region Properties

    public StructureDefinitionSO definition => _definition;
    public StructureState state => _state;
    public float hp => _hp;
    public float maxHp => _maxHp;
    public bool isDamaged => _hp < _maxHp;
    public bool isDestroyed => _state == StructureState.DESTROYED;
    
    /// <summary>True if core module is built (or no core required).</summary>
    public bool isCoreBuilt => _isCoreBuilt;
    
    /// <summary>True if structure requires core module.</summary>
    public bool requiresCore => _definition?.coreModule != null;

    public IReadOnlyList<Slot> slots => _slots;
    public IReadOnlyList<WallSegment> wallSegments => _wallSegments;
    public IReadOnlyList<EntryPoint> entryPoints => _entryPoints;
    public IReadOnlyList<GameObject> supports => _supports;

    public Vector2Int footprint => _definition != null ? _definition.footprint : Vector2Int.one;

    public List<Slot> slotsInternal => _slots;
    public List<WallSegment> wallSegmentsData => _wallSegments;
    public List<EntryPoint> entryPointsInternal => _entryPoints;
    public List<GameObject> supportsInternal => _supports;
    
    public NavMeshSurface navMeshSurface => _floorNavMeshSurface ??= GetComponentInChildren<NavMeshSurface>();

    #endregion

    #region Lifecycle

    private void OnEnable() {
      ActorRegistry<Structure>.Register(this);
    }

    private void OnDisable() {
      ActorRegistry<Structure>.Unregister(this);
    }

    private void Start() {
      if (_definition == null) {
        Debug.LogError($"[Structure] Start wasn't initialized! {name}", this);
        enabled = false;
      }
    }

    private void OnDestroy() {
      foreach (var entry in _entryPoints) entry.Destroy();
      foreach (var wall in _wallSegments) wall.DestroyInstance();
      foreach (var support in _supports) {
        if (support != null) Destroy(support);
      }
    }

    #endregion

    #region Initialization

    public void Initialize(StructureDefinitionSO definition, float maxHp = 100f) {
      _definition = definition;
      _maxHp = maxHp;
      _hp = maxHp;
      _state = StructureState.BLUEPRINT;
      _isCoreBuilt = definition.coreModule == null; // No core required = already "built"
      _cellSize = BuildingConstants.CellSize;
      
      Debug.Log($"[Structure] Initialized {name}, requiresCore: {requiresCore}");
    }

    public void SetState(StructureState newState) {
      _state = newState;
    }

    public void SetCoreBuilt(bool value) {
      _isCoreBuilt = value;
    }

    #endregion

    #region HP

    public void SetHp(float value) {
      _hp = Mathf.Clamp(value, 0, _maxHp);

      if (_hp <= 0) _state = StructureState.DESTROYED;
      else if (_hp < _maxHp && _state == StructureState.BUILT) _state = StructureState.DAMAGED;
      else if (_hp >= _maxHp && _state == StructureState.DAMAGED) _state = StructureState.BUILT;
    }

    public void ModifyHp(float delta) => SetHp(_hp + delta);

    #endregion

    #region Slot Queries

    public Slot GetSlot(string slotId) => _slots.Find(s => s.slotId == slotId);
    
    public Slot GetSlotByIndex(int index) => index >= 0 && index < _slots.Count ? _slots[index] : null;

    public Slot GetEmptySlot(SlotType type) => _slots.Find(s => s.type == type && s.isEmpty && !s.isLocked);

    public IEnumerable<Slot> GetEmptySlots() {
      foreach (var slot in _slots) {
        if (slot.isEmpty && !slot.isLocked) yield return slot;
      }
    }
    
    public IEnumerable<Slot> GetEmptySlots(SlotType type) {
      foreach (var slot in _slots) {
        if (slot.type == type && slot.isEmpty && !slot.isLocked) yield return slot;
      }
    }

    #endregion

    #region Module Placement

    /// <summary>
    /// Find slots that can fit a module. Validates footprint, clearance, and core requirement.
    /// Returns anchor slot + occupied slots, or null if cannot fit.
    /// </summary>
    public Slot[] FindSlotsForModule(ModuleDefinitionSO moduleDef, bool isCoreModule = false) {
      if (moduleDef == null) return null;
      
      // Core check: non-core modules blocked until core is built
      if (!isCoreModule && requiresCore && !_isCoreBuilt) {
        Debug.LogWarning($"[Structure] Cannot place {moduleDef.moduleId}: core module not built");
        return null;
      }
      
      var totalSlots = moduleDef.slotFootprint.x * moduleDef.slotFootprint.y;
      
      foreach (var anchorCandidate in _slots) {
        if (!anchorCandidate.isEmpty || anchorCandidate.isLocked) continue;
        if (!anchorCandidate.IsModuleCompatible(moduleDef)) continue;
        
        var result = TryFindFootprintSlots(anchorCandidate, moduleDef);
        if (result == null) continue;
        
        // Validate clearance
        if (moduleDef.clearanceRadius > 0 && !ValidateClearance(result, moduleDef.clearanceRadius)) {
          continue;
        }
        
        return result;
      }
      
      return null;
    }

    /// <summary>
    /// Check if module can be placed at specific anchor slot.
    /// </summary>
    public bool CanPlaceModule(ModuleDefinitionSO moduleDef, Slot anchorSlot, bool isCoreModule = false) {
      if (moduleDef == null || anchorSlot == null) return false;
      if (!_slots.Contains(anchorSlot)) return false;
      
      if (!isCoreModule && requiresCore && !_isCoreBuilt) return false;
      
      var result = TryFindFootprintSlots(anchorSlot, moduleDef);
      if (result == null) return false;
      
      if (moduleDef.clearanceRadius > 0 && !ValidateClearance(result, moduleDef.clearanceRadius)) {
        return false;
      }
      
      return true;
    }

    /// <summary>
    /// Assign module to slots. Returns anchor slot or null if failed.
    /// </summary>
    public Slot AssignModuleToSlots(ModuleDefinitionSO moduleDef, Slot anchorSlot = null, 
      SlotPriority priority = SlotPriority.NORMAL, bool isCoreModule = false) {
      
      Slot[] slotsToUse = anchorSlot != null 
        ? TryFindFootprintSlots(anchorSlot, moduleDef) 
        : FindSlotsForModule(moduleDef, isCoreModule);
      
      if (slotsToUse == null || slotsToUse.Length == 0) {
        Debug.LogWarning($"[Structure] Cannot assign {moduleDef.moduleId}: no suitable slots");
        return null;
      }
      
      // Validate clearance
      if (moduleDef.clearanceRadius > 0 && !ValidateClearance(slotsToUse, moduleDef.clearanceRadius)) {
        Debug.LogWarning($"[Structure] Cannot assign {moduleDef.moduleId}: clearance validation failed");
        return null;
      }
      
      var anchor = slotsToUse[0];
      anchor.AssignAsAnchor(moduleDef, priority);
      
      for (var i = 1; i < slotsToUse.Length; i++) {
        slotsToUse[i].AssignAsOccupied(anchor);
      }
      
      Debug.Log($"[Structure] Assigned {moduleDef.moduleId} to {slotsToUse.Length} slots (anchor: {anchor.slotId})");
      return anchor;
    }

    /// <summary>
    /// Get all slots occupied by a module.
    /// </summary>
    public List<Slot> GetSlotsForModule(Module module) {
      var result = new List<Slot>();
      if (module == null) return result;
      
      foreach (var slot in _slots) {
        if (slot.builtModule == module) result.Add(slot);
        else if (slot.anchorSlot?.builtModule == module) result.Add(slot);
      }
      
      return result;
    }

    /// <summary>
    /// Clear module from all its slots.
    /// </summary>
    public void ClearModule(Slot anchorSlot) {
      if (anchorSlot == null || !anchorSlot.isAnchor) return;
      
      foreach (var slot in _slots) {
        if (slot.anchorSlot == anchorSlot) slot.ClearOccupied();
      }
      
      anchorSlot.Clear();
    }

    #endregion

    #region Footprint & Clearance

    private Slot[] TryFindFootprintSlots(Slot anchor, ModuleDefinitionSO moduleDef) {
      var totalSlots = moduleDef.slotFootprint.x * moduleDef.slotFootprint.y;
      
      if (totalSlots == 1) {
        return anchor.isEmpty && !anchor.isLocked && anchor.IsModuleCompatible(moduleDef) 
          ? new[] { anchor } 
          : null;
      }
      
      var anchorPos = anchor.localPosition;
      var candidates = new List<(Slot slot, float dist)>();
      
      foreach (var slot in _slots) {
        if (!slot.isEmpty || slot.isLocked) continue;
        if (!slot.IsModuleCompatible(moduleDef)) continue;
        
        var dist = Vector3.Distance(slot.localPosition, anchorPos);
        candidates.Add((slot, dist));
      }
      
      if (candidates.Count < totalSlots) return null;
      
      candidates.Sort((a, b) => a.dist.CompareTo(b.dist));
      
      var result = new Slot[totalSlots];
      result[0] = anchor;
      
      var idx = 1;
      foreach (var (slot, _) in candidates) {
        if (slot == anchor) continue;
        if (idx >= totalSlots) break;
        result[idx++] = slot;
      }
      
      return idx == totalSlots ? result : null;
    }

    /// <summary>
    /// Validate that clearance zone around module slots is free or outside bounds.
    /// </summary>
    private bool ValidateClearance(Slot[] moduleSlots, int clearanceRadius) {
      if (clearanceRadius <= 0) return true;
      
      var modulePositions = new HashSet<Vector2Int>();
      foreach (var slot in moduleSlots) {
        modulePositions.Add(GetSlotGridPosition(slot));
      }
      
      foreach (var slot in moduleSlots) {
        var gridPos = GetSlotGridPosition(slot);
        
        for (var dx = -clearanceRadius; dx <= clearanceRadius; dx++) {
          for (var dz = -clearanceRadius; dz <= clearanceRadius; dz++) {
            if (dx == 0 && dz == 0) continue; // Skip self
            
            var checkPos = new Vector2Int(gridPos.x + dx, gridPos.y + dz);
            
            // Skip if this position is part of the module itself
            if (modulePositions.Contains(checkPos)) continue;
            
            // Skip if outside structure bounds (wall/edge)
            if (IsPositionOutsideBounds(checkPos)) continue;
            
            // Check if slot at this position is free
            var slotAtPos = GetSlotAtGridPosition(checkPos);
            if (slotAtPos != null && slotAtPos.isInUse) {
              return false; // Clearance zone blocked
            }
          }
        }
      }
      
      return true;
    }

    private Vector2Int GetSlotGridPosition(Slot slot) {
      var local = slot.localPosition;
      var x = Mathf.FloorToInt(local.x / _cellSize);
      var z = Mathf.FloorToInt(local.z / _cellSize);
      return new Vector2Int(x, z);
    }

    private Slot GetSlotAtGridPosition(Vector2Int gridPos) {
      foreach (var slot in _slots) {
        if (GetSlotGridPosition(slot) == gridPos) return slot;
      }
      return null;
    }

    private bool IsPositionOutsideBounds(Vector2Int gridPos) {
      return gridPos.x < 0 || gridPos.x >= footprint.x || 
             gridPos.y < 0 || gridPos.y >= footprint.y;
    }

    #endregion

    #region Walls

    public WallSegment GetWallSegment(WallSide side, int index) {
      return _wallSegments.Find(w => w.side == side && w.index == index);
    }

    #endregion
  }
}
