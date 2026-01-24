using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP;
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
    [Inject] private ActorCreationModule _actorCreationModule;
    [Inject] private ActorDestructionModule _actorDestructionModule;

    private readonly List<StructureDefinitionSO> _definitions = new();
    private Terrain _terrain;

    public bool isInitialized { get; private set; }
    public IReadOnlyList<StructureDefinitionSO> definitions => _definitions;

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


    /// <summary>
    /// Place a structure blueprint (UnfinishedStructure with ghost preview).
    /// </summary>
    public UnfinishedStructureActor PlaceBlueprint(StructureDefinitionSO definition, Vector3 targetPosition) {
      if (definition == null) {
        Debug.LogError("[StructuresModule] Cannot place blueprint: definition is null");
        return null;
      }

      // Calculate position on terrain
      var position = _placement.CalculateStructurePosition(targetPosition, definition.footprint, _terrain);


      if (!_actorCreationModule.TrySpawnActor("unfinished_structure", position,
            out var unfinishedStructureDescription)) {
        Debug.LogError($"[StructuresModule] Failed to spawn UnfinishedStructure actor for {definition.structureId}");
        return null;
      }

      var unfinished = unfinishedStructureDescription.GetComponent<UnfinishedStructureActor>();
      if (unfinished == null) {
        Debug.LogError(
          $"[StructuresModule] Failed to get UnfinishedStructureActor from actor {unfinishedStructureDescription.name}");
        _actorDestructionModule.DestroyActor(unfinishedStructureDescription);
        return null;
      }

      unfinished.Initialize(definition);

      //todo: rework this. Ghost is should be a regular view of UnfinishedStructureActor
      var ghost = _placement.CreateGhostView(definition, position);
      if (ghost != null) {
        ghost.transform.SetParent(unfinishedStructureDescription.transform);
        unfinished.SetGhostView(ghost);
      }

      Debug.Log($"[StructuresModule] Placed blueprint: {definition.structureId} at {position}");
      return unfinished;
    }

    public void OnStructureActorSpawned(ActorDescription actor) {
      var structure = actor.GetComponent<Structure>();
      if (structure == null) return;

      // Build walls, slots, entries
      _construction.BuildStructure(structure, _terrain);
    }

    public IEnumerable<Structure> GetAll() {
      return Registry<Structure>.GetAll();
    }

    public IEnumerable<Structure> GetByState(StructureState state) {
      foreach (Structure s in GetAll()) {
        if (s.state == state) yield return s;
      }
    }


    public Structure GetNearestWithEmptySlot(Vector3 position, SlotType slotType) {
      Structure nearest = null;
      var minDist = float.MaxValue;

      foreach (var structure in GetByState(StructureState.BUILT)) {
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

    public void SetWallSegmentType(Structure structure, WallSide side, int index, WallSegmentType newType) {
      _construction.SetWallSegmentType(structure, side, index, newType);
    }
  }
}