using System.Collections.Generic;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.Runtime;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace Content.Scripts.Building.Services {
  /// <summary>
  /// Handles the actual building of structure components (walls, slots, supports, entries).
  /// </summary>
  public class StructureConstructionService {
    [Inject] private StructurePlacementService _placement;

    /// <summary>
    /// Build all structure components. Main entry point after structure GO is created.
    /// </summary>
    public void BuildStructure(Structure structure, Terrain terrain) {
      var definition = structure.definition;
      if (definition == null) {
        Debug.LogError("[StructureConstructionService] Structure has no definition!");
        return;
      }

      // Order matters!
      SpawnFoundationView(structure);
      CreateSlots(structure);
      
      var entryPoints = _placement.DetermineEntryPoints(definition, structure.transform.position, terrain);
      structure.entryPointsInternal.AddRange(entryPoints);
      
      GenerateWalls(structure);
      GenerateSupports(structure, terrain);
      SpawnEntryPoints(structure, terrain);

      structure.SetState(StructureState.BUILT);
      Debug.Log($"[StructureConstructionService] Built structure: {definition.structureId}");
    }

    #region Foundation

    public void SpawnFoundationView(Structure structure) {
      var definition = structure.definition;
      if (definition?.foundationPrefab == null) return;

      var view = Object.Instantiate(definition.foundationPrefab, structure.transform);
      view.transform.localPosition = Vector3.zero;
      view.transform.localRotation = Quaternion.identity;
      view.name = "FoundationView";

      structure.SetFoundationView(view);
    }

    #endregion

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
      structure.wallSegmentsInternal.Clear();
      var definition = structure.definition;
      if (definition == null) return;

      // Create wall segments for each side
      CreateWallsForSide(structure, WallSide.North, definition.footprint.x);
      CreateWallsForSide(structure, WallSide.South, definition.footprint.x);
      CreateWallsForSide(structure, WallSide.East, definition.footprint.y);
      CreateWallsForSide(structure, WallSide.West, definition.footprint.y);

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

    private void CreateWallsForSide(Structure structure, WallSide side, int count) {
      for (var i = 0; i < count; i++) {
        structure.wallSegmentsInternal.Add(new WallSegment(side, i, WallSegmentType.Solid));
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

      segment.instance = Object.Instantiate(prefab, parent);
      segment.instance.transform.localPosition = localPos;
      segment.instance.transform.localRotation = localRot;
      segment.instance.name = $"Wall_{segment.side}_{segment.index}";
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
      var support = Object.Instantiate(prefab, structure.transform);
      support.transform.position = new Vector3(cellCenter.x, terrainY, cellCenter.z);
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
        SpawnStairs(entry, structure, definition);
        CreateNavMeshLink(entry, structure, definition);
      }
    }

    private void SpawnStairs(EntryPoint entry, Structure structure, StructureDefinitionSO definition) {
      if (definition.stairsPrefab == null) return;

      var structureY = structure.transform.position.y;
      var terrainY = structureY - entry.stairsHeight;

      entry.stairsInstance = Object.Instantiate(definition.stairsPrefab, structure.transform);
      entry.stairsInstance.transform.position = new Vector3(entry.stairsPosition.x, terrainY, entry.stairsPosition.z);
      entry.stairsInstance.name = $"Stairs_{entry.side}_{entry.segmentIndex}";

      // Rotate to face inward
      var rotation = entry.side switch {
        WallSide.North => Quaternion.Euler(0, 180, 0),
        WallSide.South => Quaternion.Euler(0, 0, 0),
        WallSide.East => Quaternion.Euler(0, -90, 0),
        WallSide.West => Quaternion.Euler(0, 90, 0),
        _ => Quaternion.identity
      };
      entry.stairsInstance.transform.rotation = rotation;

      // Scale Y based on height
      var scale = entry.stairsInstance.transform.localScale;
      scale.y = Mathf.Max(0.1f, entry.stairsHeight / BuildingConstants.WallHeight);
      entry.stairsInstance.transform.localScale = scale;
    }

    private void CreateNavMeshLink(EntryPoint entry, Structure structure, StructureDefinitionSO definition) {
      var structureY = structure.transform.position.y;
      var terrainY = structureY - entry.stairsHeight;
      var cellSize = BuildingConstants.CellSize;
      var footprint = definition.footprint;

      // Inside position (on structure floor)
      var insidePos = entry.side switch {
        WallSide.North => structure.transform.position + new Vector3((entry.segmentIndex + 0.5f) * cellSize, 0, (footprint.y - 0.5f) * cellSize),
        WallSide.South => structure.transform.position + new Vector3((entry.segmentIndex + 0.5f) * cellSize, 0, 0.5f * cellSize),
        WallSide.East => structure.transform.position + new Vector3((footprint.x - 0.5f) * cellSize, 0, (entry.segmentIndex + 0.5f) * cellSize),
        WallSide.West => structure.transform.position + new Vector3(0.5f * cellSize, 0, (entry.segmentIndex + 0.5f) * cellSize),
        _ => structure.transform.position
      };

      // Outside position (on terrain)
      var outsidePos = new Vector3(entry.stairsPosition.x, terrainY, entry.stairsPosition.z);

      // Create NavMeshLink
      var linkGO = new GameObject($"NavMeshLink_{entry.side}_{entry.segmentIndex}");
      linkGO.transform.SetParent(structure.transform);

      entry.navMeshLink = linkGO.AddComponent<NavMeshLink>();
      entry.navMeshLink.startPoint = linkGO.transform.InverseTransformPoint(outsidePos);
      entry.navMeshLink.endPoint = linkGO.transform.InverseTransformPoint(insidePos);
      entry.navMeshLink.width = cellSize * 0.8f;
      entry.navMeshLink.bidirectional = true;
    }

    #endregion
  }
}
