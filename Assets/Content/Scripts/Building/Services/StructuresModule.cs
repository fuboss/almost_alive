using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.Navigation;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.Data.Expansion;
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
    [Inject] private StructureExpansionService _expansion;
    [Inject] private ActorCreationModule _actorCreationModule;
    [Inject] private ActorDestructionModule _actorDestructionModule;
    [Inject] private NavigationModule _navigationModule;
    [Inject] private BuildingManagerConfigSO _config;

    private readonly List<StructureDefinitionSO> _definitions = new();
    private readonly List<ModuleDefinitionSO> _moduleDefinitions = new();
    private Terrain _terrain;

    public bool isInitialized { get; private set; }
    public bool modulesInitialized { get; private set; }
    public IReadOnlyList<StructureDefinitionSO> definitions => _definitions;
    public IReadOnlyList<ModuleDefinitionSO> moduleDefinitions => _moduleDefinitions;

    public event Action<ModuleDefinitionSO[]> OnModulesLoaded;

    void IInitializable.Initialize() {
      _terrain = Terrain.activeTerrain;

      // Load structure definitions from Addressables
      Addressables.LoadAssetsAsync<StructureDefinitionSO>("StructureDefinition").Completed += handle => {
        _definitions.AddRange(handle.Result);
        isInitialized = true;
        Debug.Log($"[StructuresModule] Loaded {_definitions.Count} structure definitions");
      };

      // Load module definitions from Addressables
      Addressables.LoadAssetsAsync<ModuleDefinitionSO>("ModuleDefinition").Completed += handle => {
        _moduleDefinitions.AddRange(handle.Result);
        modulesInitialized = true;
        Debug.Log($"[StructuresModule] Loaded {_moduleDefinitions.Count} module definitions");
        OnModulesLoaded?.Invoke(_moduleDefinitions.ToArray());
      };

      ActorRegistry<Structure>.onUnregistered += OnStructureUnregistered;
    }

    void IDisposable.Dispose() {
      ActorRegistry<Structure>.onUnregistered -= OnStructureUnregistered;
      _definitions.Clear();
    }


    /// <summary>
    /// Place a structure blueprint (UnfinishedStructure with ghost preview).
    /// </summary>
    public UnfinishedStructureActor PlaceBlueprint(StructureDefinitionSO definition, Vector3 targetPosition,
      float heightOffset = 0f) {
      if (definition == null) {
        Debug.LogError("[StructuresModule] Cannot place blueprint: definition is null");
        return null;
      }

      // Calculate position on terrain
      var position =
        _placement.CalculateStructurePosition(targetPosition, definition.footprint, _terrain, heightOffset);


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

      //todo: its a heavy operation, consider making it async
      _construction.BuildStructure(structure, _terrain); // Build walls, slots, entries
      RebuildNavMesh();
    }

    private void RebuildNavMesh() {
      _navigationModule.RequestBake();
    }

    private void OnStructureUnregistered(Structure structure) {
      if (structure != null) {
        _navigationModule.UnregisterSurface(structure.navMeshSurface);
      }

      RebuildNavMesh();
    }

    public IReadOnlyCollection<Structure> GetAll() {
      return ActorRegistry<Structure>.all;
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
    
    #region Expansion
    
    /// <summary>
    /// Place expansion at snap point of existing structure.
    /// </summary>
    public Structure PlaceExpansion(Structure baseStructure, SnapPoint snapPoint, 
      StructureDefinitionSO expansionDef) {
      
      if (baseStructure == null || snapPoint == null || expansionDef == null) {
        Debug.LogError("[StructuresModule] PlaceExpansion: invalid arguments");
        return null;
      }
      
      // Validate placement
      if (!_expansion.CanPlaceExpansion(baseStructure, snapPoint, expansionDef, _terrain)) {
        Debug.LogWarning($"[StructuresModule] Cannot place expansion {expansionDef.structureId}");
        return null;
      }
      
      // Calculate position (grid-snapped)
      var position = _expansion.CalculateExpansionPosition(baseStructure, snapPoint, expansionDef);
      
      // Spawn structure actor directly (no blueprint for expansions)
      if (!_actorCreationModule.TrySpawnActor(expansionDef.structureId, position, out var actorDesc)) {
        Debug.LogError($"[StructuresModule] Failed to spawn expansion {expansionDef.structureId}");
        return null;
      }
      
      var expansion = actorDesc.GetComponent<Structure>();
      if (expansion == null) {
        Debug.LogError($"[StructuresModule] Expansion actor missing Structure component");
        _actorDestructionModule.DestroyActor(actorDesc);
        return null;
      }
      
      // Initialize and build
      expansion.Initialize(expansionDef);
      _construction.BuildStructure(expansion, _terrain);
      
      // Create connection
      _expansion.CreateConnection(baseStructure, snapPoint, expansion);
      
      // Rebuild navmesh
      RebuildNavMesh();
      
      Debug.Log($"[StructuresModule] Placed expansion: {expansionDef.structureId} at {baseStructure.name} snap point");
      return expansion;
    }
    
    #endregion
  }
}