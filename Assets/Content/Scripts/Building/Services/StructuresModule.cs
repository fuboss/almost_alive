using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.Runtime;
using Content.Scripts.Game;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Building.Services {
  /// <summary>
  /// Main coordinator for structure system.
  /// Handles CRUD operations, queries, and orchestrates placement/construction services.
  /// </summary>
  public class StructuresModule : IInitializable, IDisposable {
    [Inject] private IObjectResolver _resolver;
    [Inject] private StructurePlacementService _placement;
    [Inject] private StructureConstructionService _construction;

    private readonly List<StructureDefinitionSO> _definitions = new();
    private Terrain _terrain;

    public bool isInitialized { get; private set; }
    public IReadOnlyList<StructureDefinitionSO> definitions => _definitions;

    #region Lifecycle

    void IInitializable.Initialize() {
      _terrain = Terrain.activeTerrain;

      // Load definitions from Addressables
      Addressables.LoadAssetsAsync<StructureDefinitionSO>("StructureDefinition").Completed += handle => {
        _definitions.AddRange(handle.Result);
        isInitialized = true;
        Debug.Log($"[StructuresModule] Loaded {_definitions.Count} structure definitions");
      };
    }

    void IDisposable.Dispose() {
      _definitions.Clear();
    }

    #endregion

    #region Blueprint Placement

    /// <summary>
    /// Place a structure blueprint (UnfinishedStructure with ghost preview).
    /// </summary>
    public UnfinishedStructure PlaceBlueprint(StructureDefinitionSO definition, Vector3 targetPosition) {
      if (definition == null) {
        Debug.LogError("[StructuresModule] Cannot place blueprint: definition is null");
        return null;
      }

      // Calculate position on terrain
      var position = _placement.CalculateStructurePosition(targetPosition, definition.footprint, _terrain);

      // Create UnfinishedStructure GO
      var go = new GameObject($"UnfinishedStructure_{definition.structureId}");
      go.transform.position = position;

      // Add required components
      go.AddComponent<ActorInventory>();
      var unfinished = go.AddComponent<UnfinishedStructure>();

      // Inject dependencies
      _resolver.Inject(go);

      // Initialize
      unfinished.Initialize(definition);

      // Create ghost preview
      var ghost = _placement.CreateGhostView(definition, position);
      if (ghost != null) {
        ghost.transform.SetParent(go.transform);
        unfinished.SetGhostView(ghost);
      }

      Debug.Log($"[StructuresModule] Placed blueprint: {definition.structureId} at {position}");
      return unfinished;
    }

    #endregion

    #region Construction Completion

    /// <summary>
    /// Complete construction — convert UnfinishedStructure to built Structure.
    /// </summary>
    public Structure CompleteConstruction(UnfinishedStructure unfinished) {
      if (unfinished == null) {
        Debug.LogError("[StructuresModule] Cannot complete: unfinished is null");
        return null;
      }

      if (!unfinished.isReadyToComplete) {
        Debug.LogWarning(
          $"[StructuresModule] Cannot complete: not ready (resources: {unfinished.hasAllResources}, work: {unfinished.workComplete})");
        return null;
      }

      var definition = unfinished.definition;
      var position = unfinished.transform.position;

      // Create Structure GO
      var go = new GameObject($"Structure_{definition.structureId}");
      go.transform.position = position;

      var structure = go.AddComponent<Structure>();
      _resolver.Inject(go);

      // Initialize structure data
      structure.Initialize(definition);

      // Build all components
      _construction.BuildStructure(structure, _terrain);

      // Destroy unfinished (ghost cleanup handled in OnDestroy)
      UnityEngine.Object.Destroy(unfinished.gameObject);

      Debug.Log($"[StructuresModule] Completed construction: {definition.structureId}");
      return structure;
    }

    #endregion

    #region Destruction

    /// <summary>
    /// Destroy a structure.
    /// </summary>
    public void DestroyStructure(Structure structure) {
      if (structure == null) return;

      Debug.Log($"[StructuresModule] Destroying structure: {structure.definition?.structureId}");
      UnityEngine.Object.Destroy(structure.gameObject);
    }

    #endregion

    #region Queries

    public StructureDefinitionSO GetDefinition(string structureId) {
      return _definitions.FirstOrDefault(d => d.structureId == structureId);
    }

    public IEnumerable<Structure> GetAll() {
      return Registry<Structure>.GetAll();
    }

    public IEnumerable<Structure> GetByState(StructureState state) {
      foreach (Structure s in GetAll()) {
        if (s.state == state) yield return s;
      }
    }

    public IEnumerable<UnfinishedStructure> GetUnfinished() {
      return Registry<UnfinishedStructure>.GetAll();
    }

    public IEnumerable<UnfinishedStructure> GetUnfinishedNeedingResources() {
      foreach (UnfinishedStructure u in Registry<UnfinishedStructure>.GetAll()) {
        if (!u.hasAllResources) yield return u;
      }
    }

    public IEnumerable<UnfinishedStructure> GetUnfinishedNeedingWork() {
      foreach (UnfinishedStructure u in Registry<UnfinishedStructure>.GetAll()) {
        if (u.hasAllResources && !u.workComplete) yield return u;
      }
    }

    public IEnumerable<UnfinishedStructure> GetUnfinishedReadyToComplete() {
      foreach (UnfinishedStructure u in Registry<UnfinishedStructure>.GetAll()) {
        if (u.isReadyToComplete) yield return u;
      }
    }

    public Structure GetNearestWithEmptySlot(Vector3 position, SlotType slotType) {
      Structure nearest = null;
      var minDist = float.MaxValue;

      foreach (var structure in GetByState(StructureState.Built)) {
        var emptySlot = structure.GetEmptySlot(slotType);
        if (emptySlot == null) continue;

        var dist = Vector3.Distance(position, structure.transform.position);
        if (dist < minDist) {
          minDist = dist;
          nearest = structure;
        }
      }

      return nearest;
    }

    #endregion

    #region Wall Management (delegated to construction service)

    public void SetWallSegmentType(Structure structure, WallSide side, int index, WallSegmentType newType) {
      _construction.SetWallSegmentType(structure, side, index, newType);
    }

    #endregion
  }
}