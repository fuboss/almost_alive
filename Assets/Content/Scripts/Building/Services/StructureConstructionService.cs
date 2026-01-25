using System.Collections.Generic;
using Content.Scripts.AI.Navigation;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.Runtime;
using Unity.AI.Navigation;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Building.Services {
  /// <summary>
  /// Handles the actual building of structure components (walls, slots, supports, entries).
  /// </summary>
  public class StructureConstructionService {
    [Inject] private StructurePlacementService _placement;
    [Inject] private NavigationModule _navigationModule;
    [Inject] private BuildingManagerConfigSO _config;

    /// <summary>
    /// Build all structure components. Main entry point after structure GO is created.
    /// </summary>
    public void BuildStructure(Structure structure, Terrain terrain) {
      var definition = structure.definition;
      if (definition == null) {
        Debug.LogError("[StructureConstructionService] Structure has no definition!");
        return;
      }

      // Always create slots
      CreateSlots(structure);
      
      // Type-specific construction
      if (definition.structureType == StructureType.Enclosed) {
        // Full enclosed structure: walls, supports, entries
        var entryPoints = _placement.DetermineEntryPoints(definition, structure.transform.position, terrain);
        structure.entryPointsInternal.AddRange(entryPoints);
        
        GenerateWalls(structure);
        GenerateSupports(structure, terrain);
        SpawnEntryPoints(structure, terrain);
      }
      else if (definition.structureType == StructureType.Open) {
        // Open structure: snap decorations to terrain
        SnapDecorationsToTerrain(structure, terrain);
      }

      _navigationModule.RegisterSurface(structure.navMeshSurface);
      
      structure.SetState(StructureState.BUILT);
      Debug.Log($"[StructureConstructionService] Built {definition.structureType} structure: {definition.structureId}");
    }
    

    #region Slots

    public void CreateSlots(Structure structure) {
      structure.slotsInternal.Clear();
      var definition = structure.definition;
      if (definition?.slots == null) return;

      foreach (var slotDef in definition.slots) {
        structure.slotsInternal.Add(new Slot(slotDef));
      }
    }

    #endregion

    #region Walls

    public void GenerateWalls(Structure structure) {
      structure.wallSegmentsData.Clear();
      var definition = structure.definition;
      if (definition == null) return;
      
      var wallsRoot = structure.wallsContainer;

      // Create side containers and wall segments
      CreateWallsForSide(structure, WallSide.North, definition.footprint.x, wallsRoot);
      CreateWallsForSide(structure, WallSide.South, definition.footprint.x, wallsRoot);
      CreateWallsForSide(structure, WallSide.East, definition.footprint.y, wallsRoot);
      CreateWallsForSide(structure, WallSide.West, definition.footprint.y, wallsRoot);

      // Mark doorways for entry points
      foreach (var entry in structure.entryPoints) {
        var segment = structure.GetWallSegment(entry.side, entry.segmentIndex);
        if (segment != null) {
          segment.type = WallSegmentType.Doorway;
        }
      }

      // Spawn wall instances
      foreach (var segment in structure.wallSegments) {
        SpawnWallInstance(segment, definition, structure.transform);
      }
    }

    private void CreateWallsForSide(Structure structure, WallSide side, int count, Transform wallsRoot) {
      var sideContainer = new GameObject(side.ToString()).transform;
      sideContainer.SetParent(wallsRoot, false);
      
      for (var i = 0; i < count; i++) {
        var segment = new WallSegment(side, i, WallSegmentType.Solid);
        segment.sideContainer = sideContainer;
        structure.wallSegmentsData.Add(segment);
      }
    }

    private void SpawnWallInstance(WallSegment segment, StructureDefinitionSO definition, Transform parent) {
      var prefab = segment.type switch {
        WallSegmentType.Solid => definition.solidWallPrefab,
        WallSegmentType.Doorway => definition.doorwayWallPrefab,
        WallSegmentType.Passage => definition.passageWallPrefab,
        _ => definition.solidWallPrefab
      };

      if (prefab == null) return;

      var localPos = segment.GetLocalPosition(definition.footprint);
      var localRot = segment.GetLocalRotation();
      
      var container = segment.sideContainer != null ? segment.sideContainer : parent;

      segment.instance = Object.Instantiate(prefab, container);
      segment.instance.transform.localPosition = localPos;
      segment.instance.transform.localRotation = localRot;
      segment.instance.name = $"Wall_{segment.index}";
    }

    /// <summary>
    /// Change wall segment type and rebuild instance.
    /// </summary>
    public void SetWallSegmentType(Structure structure, WallSide side, int index, WallSegmentType newType) {
      var segment = structure.GetWallSegment(side, index);
      if (segment == null) return;

      segment.DestroyInstance();
      segment.type = newType;
      SpawnWallInstance(segment, structure.definition, structure.transform);
    }

    #endregion

    #region Supports

    public void GenerateSupports(Structure structure, Terrain terrain) {
      structure.supportsInternal.Clear();
      var definition = structure.definition;
      if (definition?.supportPrefab == null || terrain == null) return;

      var cellSize = BuildingConstants.CellSize;
      var origin = structure.transform.position;
      var structureY = origin.y;

      var heights = _placement.GetTerrainHeightsGrid(origin, definition.footprint, terrain);

      for (var x = 0; x < definition.footprint.x; x++) {
        for (var z = 0; z < definition.footprint.y; z++) {
          var terrainY = heights[x, z];
          var gap = structureY - terrainY;

          if (gap > 0.1f) {
            var cellCenter = origin + new Vector3((x + 0.5f) * cellSize, 0, (z + 0.5f) * cellSize);
            SpawnSupport(structure, cellCenter, terrainY, gap, definition.supportPrefab);
          }
        }
      }
    }

    private void SpawnSupport(Structure structure, Vector3 cellCenter, float terrainY, float height, GameObject prefab) {
      var support = Object.Instantiate(prefab, structure.supportsContainer);
      support.transform.position = new Vector3(cellCenter.x, terrainY + height/2, cellCenter.z);
      support.name = $"Support_{structure.supportsInternal.Count}";

      // Scale Y to match gap
      var scale = support.transform.localScale;
      scale.y = height;
      support.transform.localScale = scale;

      structure.supportsInternal.Add(support);
    }

    #endregion

    #region Entry Points

    public void SpawnEntryPoints(Structure structure, Terrain terrain) {
      var definition = structure.definition;
      if (definition == null) return;

      foreach (var entry in structure.entryPoints) {
        var entryContainer = new GameObject($"Entry_{entry.side}_{entry.segmentIndex}").transform;
        entryContainer.SetParent(structure.entriesContainer, false);
        
        SpawnStairs(entry, structure, definition, entryContainer);
        CreateNavMeshLink(entry, structure, definition, entryContainer);
      }
    }

    private void SpawnStairs(EntryPoint entry, Structure structure, StructureDefinitionSO definition, Transform container) {
      if (definition.stairsPrefab == null) return;

      var structureY = structure.transform.position.y;

      entry.stairsInstance = Object.Instantiate(definition.stairsPrefab);
      entry.stairsInstance.transform.position = new Vector3(entry.stairsPosition.x, structureY, entry.stairsPosition.z);
      entry.stairsInstance.name = "Stairs";

      // Rotate to face inward
      var rotation = entry.side switch {
        WallSide.North => Quaternion.Euler(0, 0, 0),
        WallSide.South => Quaternion.Euler(0, 180, 0),
        WallSide.East => Quaternion.Euler(0, 90, 0),
        WallSide.West => Quaternion.Euler(0, -90, 0),
        _ => Quaternion.identity
      };
      entry.stairsInstance.transform.rotation = rotation;
      
      // Parent with world position stays (stairs already positioned correctly)
      entry.stairsInstance.transform.SetParent(container, true);
    }

    private void CreateNavMeshLink(EntryPoint entry, Structure structure, StructureDefinitionSO definition, Transform container) {
      var cellSize = BuildingConstants.CellSize;
      var footprint = definition.footprint;

      // Wall edge position (at doorway)
      var wallEdgePos = entry.side switch {
        WallSide.North => new Vector3((entry.segmentIndex + 0.5f) * cellSize, 0, footprint.y * cellSize),
        WallSide.South => new Vector3((entry.segmentIndex + 0.5f) * cellSize, 0, 0),
        WallSide.East => new Vector3(footprint.x * cellSize, 0, (entry.segmentIndex + 0.5f) * cellSize),
        WallSide.West => new Vector3(0, 0, (entry.segmentIndex + 0.5f) * cellSize),
        _ => Vector3.zero
      };

      // Offset directions
      var offsetDir = entry.side switch {
        WallSide.North => Vector3.forward,
        WallSide.South => Vector3.back,
        WallSide.East => Vector3.right,
        WallSide.West => Vector3.left,
        _ => Vector3.zero
      };
      
      // Inside position (1 cell inward from wall edge)
      var localInsidePos = wallEdgePos - offsetDir * cellSize;
      
      // Outside position (1 cell outward from wall edge + height drop)
      var localOutsidePos = wallEdgePos + offsetDir * cellSize - Vector3.up * entry.stairsHeight;

      // Create NavMeshLink (parented to structure, uses local coords)
      var linkGO = new GameObject("NavMeshLink");
      linkGO.transform.SetParent(container, false);

      entry.navMeshLink = linkGO.AddComponent<NavMeshLink>();
      entry.navMeshLink.startPoint = localOutsidePos;
      entry.navMeshLink.endPoint = localInsidePos;
      entry.navMeshLink.width = cellSize * 0.8f;
      entry.navMeshLink.bidirectional = true;
    }

    #endregion

    #region Open Structures

    /// <summary>
    /// Snap all decorations in foundation to terrain surface.
    /// Searches for TerrainSnapDecoration components in structure hierarchy.
    /// </summary>
    private void SnapDecorationsToTerrain(Structure structure, Terrain terrain) {
      if (terrain == null) {
        Debug.LogWarning("[StructureConstructionService] No terrain for decoration snap");
        return;
      }

      var decorations = structure.GetComponentsInChildren<TerrainSnapDecoration>(true);
      if (decorations.Length == 0) {
        Debug.LogWarning($"[StructureConstructionService] Open structure {structure.name} has no TerrainSnapDecoration components");
        return;
      }

      foreach (var decoration in decorations) {
        decoration.SnapToTerrain(terrain);
      }

      Debug.Log($"[StructureConstructionService] Snapped {decorations.Length} decorations to terrain");
    }

    #endregion
  }
}
