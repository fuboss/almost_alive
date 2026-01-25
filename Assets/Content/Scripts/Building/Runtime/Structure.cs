using System;
using System.Collections.Generic;
using Content.Scripts.Building.Data;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Structure state machine.
  /// </summary>
  public enum StructureState {
    BLUEPRINT, // placed, awaiting construction
    UNDER_CONSTRUCTION, // being built
    BUILT, // functional
    DAMAGED, // needs repair
    DESTROYED // gone
  }

  /// <summary>
  /// Runtime structure instance in the world.
  /// Data-only — all logic handled by services.
  /// </summary>
  public class Structure : MonoBehaviour {
    [Title("Runtime State")] [ShowInInspector, ReadOnly]
    private StructureDefinitionSO _definition;

    [ShowInInspector, ReadOnly] private StructureState _state = StructureState.BLUEPRINT;

    [ShowInInspector, ReadOnly] private float _hp;

    [ShowInInspector, ReadOnly] private float _maxHp = 100f;

    [Title("Runtime Data")] [ShowInInspector, ReadOnly]
    private readonly List<Slot> _slots = new();

    [ShowInInspector, ReadOnly] private readonly List<WallSegment> _wallSegments = new();

    [ShowInInspector, ReadOnly] private readonly List<EntryPoint> _entryPoints = new();

    [ShowInInspector, ReadOnly] private readonly List<GameObject> _supports = new();
    private NavMeshSurface _floorNavMeshSurface;

    #region Properties

    public StructureDefinitionSO definition => _definition;
    public StructureState state => _state;
    public float hp => _hp;
    public float maxHp => _maxHp;
    public bool isDamaged => _hp < _maxHp;
    public bool isDestroyed => _state == StructureState.DESTROYED;

    public IReadOnlyList<Slot> slots => _slots;
    public IReadOnlyList<WallSegment> wallSegments => _wallSegments;
    public IReadOnlyList<EntryPoint> entryPoints => _entryPoints;
    public IReadOnlyList<GameObject> supports => _supports;

    public Vector2Int footprint => _definition != null ? _definition.footprint : Vector2Int.one;

    // Mutable lists for services to populate
    public List<Slot> slotsInternal => _slots;
    public List<WallSegment> wallSegmentsData => _wallSegments;
    public List<EntryPoint> entryPointsInternal => _entryPoints;
    public List<GameObject> supportsInternal => _supports;
    
    public NavMeshSurface navMeshSurface => _floorNavMeshSurface??= GetComponentInChildren<NavMeshSurface>();

    #endregion

    private void OnEnable() {
      ActorRegistry<Structure>.Register(this);
    }

    private void OnDisable() {
      ActorRegistry<Structure>.Unregister(this);
    }

    private void Start() {
      if (_definition == null) {
        Debug.LogError($"[Structure]Start wasn't initialized! {name}", this);
        enabled = false;
      }
    }


    public void Initialize(StructureDefinitionSO definition, float maxHp = 100f) {
      Debug.LogError($"[Structure]initialized! {name}", this);
      _definition = definition;
      _maxHp = maxHp;
      _hp = maxHp;
      _state = StructureState.BLUEPRINT;
    }

    public void SetState(StructureState newState) {
      _state = newState;
    }

    public void SetHp(float value) {
      _hp = Mathf.Clamp(value, 0, _maxHp);

      if (_hp <= 0) {
        _state = StructureState.DESTROYED;
      }
      else if (_hp < _maxHp && _state == StructureState.BUILT) {
        _state = StructureState.DAMAGED;
      }
      else if (_hp >= _maxHp && _state == StructureState.DAMAGED) {
        _state = StructureState.BUILT;
      }
    }

    public void ModifyHp(float delta) {
      SetHp(_hp + delta);
    }

    public Slot GetSlot(string slotId) {
      return _slots.Find(s => s.slotId == slotId);
    }

    public Slot GetEmptySlot(SlotType type) {
      return _slots.Find(s => s.type == type && s.isEmpty && !s.isLocked);
    }

    public IEnumerable<Slot> GetEmptySlots() {
      foreach (var slot in _slots) {
        if (slot.isEmpty && !slot.isLocked) yield return slot;
      }
    }

    public WallSegment GetWallSegment(WallSide side, int index) {
      return _wallSegments.Find(w => w.side == side && w.index == index);
    }

    private void OnDestroy() {
      // Cleanup entry points
      foreach (var entry in _entryPoints) {
        entry.Destroy();
      }

      // Cleanup walls
      foreach (var wall in _wallSegments) {
        wall.DestroyInstance();
      }

      // Cleanup supports
      foreach (var support in _supports) {
        if (support != null) Destroy(support);
      }
    }
  }
}