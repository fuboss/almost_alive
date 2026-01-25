using Content.Scripts.Building.Data;
using Content.Scripts.Building.Data.Expansion;
using Content.Scripts.Building.Runtime;
using Content.Scripts.World.Grid;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Building.Services {
  /// <summary>
  /// Handles structure expansion: snap point positioning, connection creation, wall updates.
  /// </summary>
  public class StructureExpansionService {
    [Inject] private StructurePlacementService _placement;
    [Inject] private StructureConstructionService _construction;

    /// <summary>
    /// Calculate world position for expansion at snap point.
    /// Result is aligned to WorldGrid.
    /// </summary>
    public Vector3 CalculateExpansionPosition(Structure baseStructure, SnapPoint snapPoint, 
      StructureDefinitionSO expansionDef) {
      
      if (baseStructure == null || snapPoint == null || expansionDef == null) {
        return Vector3.zero;
      }

      var cellSize = BuildingConstants.CellSize;
      var basePos = baseStructure.transform.position;
      var baseFootprint = baseStructure.footprint;

      // Wall edge position (local to base structure)
      var edgeOffset = snapPoint.side switch {
        WallSide.North => new Vector3(snapPoint.segmentIndex * cellSize, 0, baseFootprint.y * cellSize),
        WallSide.South => new Vector3(snapPoint.segmentIndex * cellSize, 0, 0),
        WallSide.East => new Vector3(baseFootprint.x * cellSize, 0, snapPoint.segmentIndex * cellSize),
        WallSide.West => new Vector3(0, 0, snapPoint.segmentIndex * cellSize),
        _ => Vector3.zero
      };

      // Offset by expansion footprint
      var expansionOffset = snapPoint.side switch {
        WallSide.North => new Vector3(0, 0, cellSize),
        WallSide.South => new Vector3(0, 0, -expansionDef.footprint.y * cellSize),
        WallSide.East => new Vector3(cellSize, 0, 0),
        WallSide.West => new Vector3(-expansionDef.footprint.x * cellSize, 0, 0),
        _ => Vector3.zero
      };

      var rawPos = basePos + edgeOffset + expansionOffset;

      // Snap to WorldGrid
      return WorldGrid.SnapToGrid(rawPos);
    }

    /// <summary>
    /// Validate expansion placement at snap point.
    /// </summary>
    public bool CanPlaceExpansion(Structure baseStructure, SnapPoint snapPoint, 
      StructureDefinitionSO expansionDef, Terrain terrain) {
      
      if (baseStructure == null || snapPoint == null || expansionDef == null) {
        return false;
      }

      // Check if expansion is allowed at this snap point
      if (!snapPoint.IsExpansionAllowed(expansionDef)) {
        Debug.LogWarning($"[StructureExpansionService] Expansion {expansionDef.structureId} not allowed at snap point");
        return false;
      }

      // Calculate position
      var position = CalculateExpansionPosition(baseStructure, snapPoint, expansionDef);

      // Validate terrain, obstacles, existing structures
      var cellSize = BuildingConstants.CellSize;
      var footprint = expansionDef.footprint;

      // Check for overlapping structures
      var existingStructures = Object.FindObjectsOfType<Structure>();
      foreach (var existing in existingStructures) {
        if (existing == baseStructure) continue;
        
        // Check footprint overlap
        var existingBounds = new Bounds(
          existing.transform.position + new Vector3(
            existing.footprint.x * cellSize * 0.5f, 0, 
            existing.footprint.y * cellSize * 0.5f),
          new Vector3(existing.footprint.x * cellSize, 10f, existing.footprint.y * cellSize)
        );
        
        var expansionBounds = new Bounds(
          position + new Vector3(
            footprint.x * cellSize * 0.5f, 0, 
            footprint.y * cellSize * 0.5f),
          new Vector3(footprint.x * cellSize, 10f, footprint.y * cellSize)
        );
        
        if (existingBounds.Intersects(expansionBounds)) {
          Debug.LogWarning($"[StructureExpansionService] Expansion overlaps with existing structure");
          return false;
        }
      }

      return true;
    }

    /// <summary>
    /// Create connection between structures and update walls/entries.
    /// </summary>
    public StructureConnection CreateConnection(Structure baseStructure, SnapPoint snapPoint, 
      Structure expansion) {
      
      if (baseStructure == null || snapPoint == null || expansion == null) {
        return null;
      }

      var targetWall = GetOppositeSide(snapPoint.side);
      var targetSegmentIndex = CalculateMatchingSegment(snapPoint, expansion);

      var connection = new StructureConnection(
        baseStructure, expansion,
        snapPoint.side, snapPoint.segmentIndex,
        targetWall, targetSegmentIndex
      );

      baseStructure.connectionsInternal.Add(connection);
      expansion.connectionsInternal.Add(connection);

      UpdateConnectionWalls(connection);

      Debug.Log($"[StructureExpansionService] Created connection: {baseStructure.name} ({snapPoint.side}:{snapPoint.segmentIndex}) <-> {expansion.name} ({targetWall}:{targetSegmentIndex})");

      return connection;
    }

    /// <summary>
    /// Update walls at connection points: change to passages, remove entries.
    /// </summary>
    public void UpdateConnectionWalls(StructureConnection connection) {
      if (connection == null || !connection.IsValid()) return;

      // Change walls to passages
      _construction.SetWallSegmentType(
        connection.sourceStructure,
        connection.sourceWall,
        connection.sourceSegmentIndex,
        WallSegmentType.Passage
      );

      _construction.SetWallSegmentType(
        connection.targetStructure,
        connection.targetWall,
        connection.targetSegmentIndex,
        WallSegmentType.Passage
      );

      // Remove entries at connection points
      RemoveEntryAtSegment(connection.sourceStructure, connection.sourceWall, connection.sourceSegmentIndex);
      RemoveEntryAtSegment(connection.targetStructure, connection.targetWall, connection.targetSegmentIndex);

      connection.isPassageCreated = true;
    }

    /// <summary>
    /// Remove entry (stairs + navmesh link) at specific wall segment.
    /// </summary>
    public void RemoveEntryAtSegment(Structure structure, WallSide side, int segmentIndex) {
      if (structure == null) return;

      for (var i = structure.entryPointsInternal.Count - 1; i >= 0; i--) {
        var entry = structure.entryPointsInternal[i];
        if (entry.side == side && entry.segmentIndex == segmentIndex) {
          entry.Destroy();
          structure.entryPointsInternal.RemoveAt(i);
          Debug.Log($"[StructureExpansionService] Removed entry at {structure.name} {side}:{segmentIndex}");
        }
      }
    }

    #region Helpers

    private WallSide GetOppositeSide(WallSide side) {
      return side switch {
        WallSide.North => WallSide.South,
        WallSide.South => WallSide.North,
        WallSide.East => WallSide.West,
        WallSide.West => WallSide.East,
        _ => side
      };
    }

    private int CalculateMatchingSegment(SnapPoint snapPoint, Structure expansion) {
      // For now, assume simple 1:1 mapping
      // Later can add offset logic if expansions have different segment counts
      var targetSegmentIndex = snapPoint.side switch {
        WallSide.North => snapPoint.segmentIndex,
        WallSide.South => snapPoint.segmentIndex,
        WallSide.East => snapPoint.segmentIndex,
        WallSide.West => snapPoint.segmentIndex,
        _ => 0
      };

      // Clamp to valid range
      var maxSegments = snapPoint.side switch {
        WallSide.North or WallSide.South => expansion.footprint.x - 1,
        WallSide.East or WallSide.West => expansion.footprint.y - 1,
        _ => 0
      };

      return Mathf.Clamp(targetSegmentIndex, 0, maxSegments);
    }

    #endregion
  }
}
