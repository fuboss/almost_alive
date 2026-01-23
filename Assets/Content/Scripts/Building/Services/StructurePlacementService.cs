using System.Collections.Generic;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.Runtime;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Building.Services {
  /// <summary>
  /// Handles terrain positioning and ghost preview for structure placement.
  /// </summary>
  public class StructurePlacementService {
    [Inject] [Key("ghostMaterial")] private Material _ghostMaterial;

    /// <summary>
    /// Calculate structure position on terrain (at max height within footprint).
    /// </summary>
    public Vector3 CalculateStructurePosition(Vector3 targetPos, Vector2Int footprint, Terrain terrain) {
      if (terrain == null) return targetPos;

      var maxHeight = GetMaxTerrainHeight(targetPos, footprint, terrain);
      return new Vector3(targetPos.x, maxHeight, targetPos.z);
    }

    /// <summary>
    /// Get maximum terrain height within footprint area.
    /// </summary>
    public float GetMaxTerrainHeight(Vector3 origin, Vector2Int footprint, Terrain terrain) {
      if (terrain == null) return origin.y;

      var cellSize = BuildingConstants.CellSize;
      var maxHeight = float.MinValue;

      for (var x = 0; x < footprint.x; x++) {
        for (var z = 0; z < footprint.y; z++) {
          var samplePos = origin + new Vector3((x + 0.5f) * cellSize, 0, (z + 0.5f) * cellSize);
          var height = terrain.SampleHeight(samplePos) + terrain.transform.position.y;
          maxHeight = Mathf.Max(maxHeight, height);
        }
      }

      return maxHeight;
    }

    /// <summary>
    /// Create ghost preview of the structure with transparent material.
    /// </summary>
    public GameObject CreateGhostView(StructureDefinitionSO definition, Vector3 position) {
      if (definition.foundationPrefab == null) return null;

      var ghost = Object.Instantiate(definition.foundationPrefab, position, Quaternion.identity);
      ghost.name = $"{definition.structureId}_Ghost";

      // Apply ghost material to all renderers
      if (_ghostMaterial != null) {
        ApplyGhostMaterial(ghost, _ghostMaterial);
      }

      // Disable colliders on ghost
      DisableColliders(ghost);

      return ghost;
    }

    /// <summary>
    /// Determine entry points based on terrain heights around structure perimeter.
    /// </summary>
    public List<EntryPoint> DetermineEntryPoints(StructureDefinitionSO definition, Vector3 structurePos,
      Terrain terrain) {
      var entryPoints = new List<EntryPoint>();
      if (terrain == null || definition == null) return entryPoints;

      var cellSize = BuildingConstants.CellSize;
      var structureY = structurePos.y;

      CheckEntryDirection(entryPoints, definition, WallSide.North, EntryDirection.North, structurePos, cellSize,
        structureY, terrain);
      CheckEntryDirection(entryPoints, definition, WallSide.South, EntryDirection.South, structurePos, cellSize,
        structureY, terrain);
      CheckEntryDirection(entryPoints, definition, WallSide.East, EntryDirection.East, structurePos, cellSize,
        structureY, terrain);
      CheckEntryDirection(entryPoints, definition, WallSide.West, EntryDirection.West, structurePos, cellSize,
        structureY, terrain);

      return entryPoints;
    }

    /// <summary>
    /// Get terrain heights for each cell in footprint (for support generation).
    /// </summary>
    public float[,] GetTerrainHeightsGrid(Vector3 origin, Vector2Int footprint, Terrain terrain) {
      var heights = new float[footprint.x, footprint.y];
      if (terrain == null) return heights;

      var cellSize = BuildingConstants.CellSize;

      for (var x = 0; x < footprint.x; x++) {
        for (var z = 0; z < footprint.y; z++) {
          var samplePos = origin + new Vector3((x + 0.5f) * cellSize, 0, (z + 0.5f) * cellSize);
          heights[x, z] = terrain.SampleHeight(samplePos) + terrain.transform.position.y;
        }
      }

      return heights;
    }

    #region Private Helpers

    private void CheckEntryDirection(
      List<EntryPoint> entryPoints,
      StructureDefinitionSO definition,
      WallSide side,
      EntryDirection dirFlag,
      Vector3 origin,
      float cellSize,
      float structureY,
      Terrain terrain
    ) {
      if ((definition.entryDirections & dirFlag) == 0) return;

      var segmentCount = GetSegmentCount(side, definition.footprint);
      var bestSegment = -1;
      var minGap = float.MaxValue;
      var bestTerrainY = 0f;
      var bestPosition = Vector3.zero;

      for (var i = 0; i < segmentCount; i++) {
        var checkPos = GetOutsidePosition(side, i, origin, cellSize, definition.footprint);
        var terrainY = terrain.SampleHeight(checkPos) + terrain.transform.position.y;
        var gap = structureY - terrainY;

        if (gap >= 0 && gap < minGap && gap <= definition.maxStairsHeight) {
          minGap = gap;
          bestSegment = i;
          bestTerrainY = terrainY;
          bestPosition = checkPos;
        }
      }

      if (bestSegment >= 0) {
        var entry = new EntryPoint(side, bestSegment, bestPosition, minGap);
        entryPoints.Add(entry);
      }
    }

    private Vector3 GetOutsidePosition(WallSide side, int index, Vector3 origin, float cellSize, Vector2Int footprint) {
      var offset = 0.5f;
      return side switch {
        WallSide.North => origin + new Vector3((index + 0.5f) * cellSize, 0, footprint.y * cellSize + offset),
        WallSide.South => origin + new Vector3((index + 0.5f) * cellSize, 0, -offset),
        WallSide.East => origin + new Vector3(footprint.x * cellSize + offset, 0, (index + 0.5f) * cellSize),
        WallSide.West => origin + new Vector3(-offset, 0, (index + 0.5f) * cellSize),
        _ => origin
      };
    }

    private int GetSegmentCount(WallSide side, Vector2Int footprint) {
      return side switch {
        WallSide.North or WallSide.South => footprint.x,
        WallSide.East or WallSide.West => footprint.y,
        _ => 0
      };
    }

    private void ApplyGhostMaterial(GameObject root, Material material) {
      var renderers = root.GetComponentsInChildren<Renderer>(true);
      foreach (var renderer in renderers) {
        var materials = new Material[renderer.sharedMaterials.Length];
        for (var i = 0; i < materials.Length; i++) {
          materials[i] = material;
        }

        renderer.materials = materials;
      }
    }

    private void DisableColliders(GameObject root) {
      var colliders = root.GetComponentsInChildren<Collider>(true);
      foreach (var collider in colliders) {
        collider.enabled = false;
      }
    }

    #endregion
  }
}