using System.Collections.Generic;
using Content.Scripts.Building.Data;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Structure state machine.
  /// </summary>
  public enum StructureState {
    Blueprint,          // placed, awaiting construction
    UnderConstruction,  // being built
    Built,              // functional
    Damaged,            // needs repair
    Destroyed           // gone
  }

  /// <summary>
  /// Runtime structure instance in the world.
  /// Data-only — all logic handled by services.
  /// </summary>
  public class Structure : MonoBehaviour {
    [Title("Runtime State")]
    [ShowInInspector, ReadOnly] 
    private StructureDefinitionSO _definition;
    
    [ShowInInspector, ReadOnly] 
    private StructureState _state = StructureState.Blueprint;
    
    [ShowInInspector, ReadOnly] 
    private float _hp;
    
    [ShowInInspector, ReadOnly] 
    private float _maxHp = 100f;

    [Title("Runtime Data")]
    [ShowInInspector, ReadOnly] 
    private readonly List<Slot> _slots = new();
    
    [ShowInInspector, ReadOnly] 
    private readonly List<WallSegment> _wallSegments = new();
    
    [ShowInInspector, ReadOnly] 
    private readonly List<EntryPoint> _entryPoints = new();
    
    [ShowInInspector, ReadOnly] 
    private readonly List<GameObject> _supports = new();

    [ShowInInspector, ReadOnly]
    private GameObject _foundationView;

    #region Properties

    public StructureDefinitionSO definition => _definition;
    public StructureState state => _state;
    public float hp => _hp;
    public float maxHp => _maxHp;
    public bool isDamaged => _hp < _maxHp;
    public bool isDestroyed => _state == StructureState.Destroyed;
    
    public IReadOnlyList<Slot> slots => _slots;
    public IReadOnlyList<WallSegment> wallSegments => _wallSegments;
    public IReadOnlyList<EntryPoint> entryPoints => _entryPoints;
    public IReadOnlyList<GameObject> supports => _supports;
    public GameObject foundationView => _foundationView;
    
    public Vector2Int footprint => _definition != null ? _definition.footprint : Vector2Int.one;

    // Mutable lists for services to populate
    public List<Slot> slotsInternal => _slots;
    public List<WallSegment> wallSegmentsInternal => _wallSegments;
    public List<EntryPoint> entryPointsInternal => _entryPoints;
    public List<GameObject> supportsInternal => _supports;

    #endregion

    #region Lifecycle

    private void OnEnable() {
      Registry<Structure>.Register(this);
    }

    private void OnDisable() {
      Registry<Structure>.Unregister(this);
    }

    #endregion

    #region Setters (for services)

    public void Initialize(StructureDefinitionSO definition, float maxHp = 100f) {
      _definition = definition;
      _maxHp = maxHp;
      _hp = maxHp;
      _state = StructureState.Blueprint;
    }

    public void SetFoundationView(GameObject view) {
      _foundationView = view;
    }

    public void SetState(StructureState newState) {
      _state = newState;
    }

    public void SetHp(float value) {
      _hp = Mathf.Clamp(value, 0, _maxHp);
      
      if (_hp <= 0) {
        _state = StructureState.Destroyed;
      }
      else if (_hp < _maxHp && _state == StructureState.Built) {
        _state = StructureState.Damaged;
      }
      else if (_hp >= _maxHp && _state == StructureState.Damaged) {
        _state = StructureState.Built;
      }
    }

    public void ModifyHp(float delta) {
      SetHp(_hp + delta);
    }

    #endregion

    #region Queries

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

    #endregion

    #region Cleanup

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

      // Cleanup foundation
      if (_foundationView != null) {
        Destroy(_foundationView);
      }
    }

    #endregion
  }
}
