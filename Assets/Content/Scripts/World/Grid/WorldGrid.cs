using System.Collections.Generic;
using Content.Scripts.Core;
using Content.Scripts.Game;
using UnityEngine;

namespace Content.Scripts.World.Grid {
  /// <summary>
  /// Spatial index for actors using 2D grid cells.
  /// </summary>
  public static class WorldGrid {
    public static float cellSize = 1f;

    // coord -> actors in that cell
    private static readonly Dictionary<GroundCoord, HashSet<ActorDescription>> _cells = new();

    // actor -> cells it occupies (for fast unregister/update)
    private static readonly Dictionary<ActorDescription, HashSet<GroundCoord>> _actorCells = new();

    static WorldGrid() {
      StaticResetRegistry.RegisterReset(Clear);
    }

    #region Registration

    public static void Register(ActorDescription actor) {
      if (actor == null) return;
      if (_actorCells.ContainsKey(actor)) return; // already registered

      var cells = CalculateOccupiedCells(actor);
      _actorCells[actor] = cells;

      foreach (var coord in cells) {
        GetOrCreateCell(coord).Add(actor);
      }
    }

    public static void Unregister(ActorDescription actor) {
      if (actor == null) return;
      if (!_actorCells.TryGetValue(actor, out var cells)) return;

      foreach (var coord in cells) {
        if (_cells.TryGetValue(coord, out var set)) {
          set.Remove(actor);
          if (set.Count == 0) _cells.Remove(coord);
        }
      }

      _actorCells.Remove(actor);
    }

    public static void UpdatePosition(ActorDescription actor) {
      if (actor == null) return;

      var newCells = CalculateOccupiedCells(actor);

      if (!_actorCells.TryGetValue(actor, out var oldCells)) {
        // Not registered yet, just register
        _actorCells[actor] = newCells;
        foreach (var coord in newCells) {
          GetOrCreateCell(coord).Add(actor);
        }
        return;
      }

      // Diff-based update
      // Remove from cells no longer occupied
      foreach (var coord in oldCells) {
        if (!newCells.Contains(coord)) {
          if (_cells.TryGetValue(coord, out var set)) {
            set.Remove(actor);
            if (set.Count == 0) _cells.Remove(coord);
          }
        }
      }

      // Add to new cells
      foreach (var coord in newCells) {
        if (!oldCells.Contains(coord)) {
          GetOrCreateCell(coord).Add(actor);
        }
      }

      _actorCells[actor] = newCells;
    }

    #endregion

    #region Queries

    /// <summary>
    /// Get all actors in exact cell.
    /// </summary>
    public static HashSet<ActorDescription> GetActorsAt(GroundCoord coord) {
      return _cells.TryGetValue(coord, out var set) ? set : null;
    }

    /// <summary>
    /// Get all actors within Chebyshev radius (square area).
    /// </summary>
    public static IEnumerable<ActorDescription> GetActorsInRadius(GroundCoord center, int radius) {
      var seen = new HashSet<ActorDescription>();

      for (var dx = -radius; dx <= radius; dx++) {
        for (var dz = -radius; dz <= radius; dz++) {
          var coord = new GroundCoord(center.x + dx, center.z + dz);
          if (!_cells.TryGetValue(coord, out var set)) continue;

          foreach (var actor in set) {
            if (seen.Add(actor)) yield return actor;
          }
        }
      }
    }

    /// <summary>
    /// Get all actors in rectangular area (inclusive).
    /// </summary>
    public static IEnumerable<ActorDescription> GetActorsInRect(GroundCoord min, GroundCoord max) {
      var seen = new HashSet<ActorDescription>();

      var minX = Mathf.Min(min.x, max.x);
      var maxX = Mathf.Max(min.x, max.x);
      var minZ = Mathf.Min(min.z, max.z);
      var maxZ = Mathf.Max(min.z, max.z);

      for (var x = minX; x <= maxX; x++) {
        for (var z = minZ; z <= maxZ; z++) {
          var coord = new GroundCoord(x, z);
          if (!_cells.TryGetValue(coord, out var set)) continue;

          foreach (var actor in set) {
            if (seen.Add(actor)) yield return actor;
          }
        }
      }
    }

    /// <summary>
    /// Get all actors within radius that have specific tag.
    /// </summary>
    public static IEnumerable<ActorDescription> GetActorsInRadius(GroundCoord center, int radius, string tag) {
      foreach (var actor in GetActorsInRadius(center, radius)) {
        if (actor.HasTag(tag)) yield return actor;
      }
    }

    /// <summary>
    /// Check if any actor with tag exists in radius.
    /// </summary>
    public static bool HasActorInRadius(GroundCoord center, int radius, string tag) {
      for (var dx = -radius; dx <= radius; dx++) {
        for (var dz = -radius; dz <= radius; dz++) {
          var coord = new GroundCoord(center.x + dx, center.z + dz);
          if (!_cells.TryGetValue(coord, out var set)) continue;

          foreach (var actor in set) {
            if (actor.HasTag(tag)) return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Find nearest actor with tag within radius. Returns null if none found.
    /// </summary>
    public static ActorDescription GetNearestInRadius(GroundCoord center, int radius, string tag) {
      ActorDescription nearest = null;
      var nearestDist = int.MaxValue;

      for (var dx = -radius; dx <= radius; dx++) {
        for (var dz = -radius; dz <= radius; dz++) {
          var coord = new GroundCoord(center.x + dx, center.z + dz);
          if (!_cells.TryGetValue(coord, out var set)) continue;

          var dist = center.ChebyshevDistance(coord);
          if (dist >= nearestDist) continue;

          foreach (var actor in set) {
            if (!actor.HasTag(tag)) continue;
            nearest = actor;
            nearestDist = dist;
            break; // found one in this cell, check closer cells
          }
        }
      }

      return nearest;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Get cells occupied by actor based on renderer bounds.
    /// </summary>
    public static HashSet<GroundCoord> GetOccupiedCells(ActorDescription actor) {
      return _actorCells.TryGetValue(actor, out var cells) ? cells : CalculateOccupiedCells(actor);
    }

    public static void Clear() {
      _cells.Clear();
      _actorCells.Clear();
    }

    public static int CellCount => _cells.Count;
    public static int ActorCount => _actorCells.Count;

    #endregion

    #region Private

    private static HashSet<ActorDescription> GetOrCreateCell(GroundCoord coord) {
      if (!_cells.TryGetValue(coord, out var set)) {
        set = new HashSet<ActorDescription>();
        _cells[coord] = set;
      }
      return set;
    }

    private static HashSet<GroundCoord> CalculateOccupiedCells(ActorDescription actor) {
      var result = new HashSet<GroundCoord>();

      var renderer = actor.GetComponentInChildren<Renderer>();
      if (renderer == null) {
        // No renderer - use single cell at position
        result.Add(GroundCoord.FromWorld(actor.transform.position));
        return result;
      }

      var bounds = renderer.bounds;
      var min = GroundCoord.FromWorld(bounds.min);
      var max = GroundCoord.FromWorld(bounds.max);

      for (var x = min.x; x <= max.x; x++) {
        for (var z = min.z; z <= max.z; z++) {
          result.Add(new GroundCoord(x, z));
        }
      }

      return result;
    }

    #endregion

    #region Snapping

    /// <summary>
    /// Snap world position to grid alignment.
    /// X and Z are aligned to cellSize grid, Y remains unchanged.
    /// </summary>
    public static Vector3 SnapToGrid(Vector3 worldPosition) {
      var snappedX = Mathf.Floor(worldPosition.x / cellSize) * cellSize;
      var snappedZ = Mathf.Floor(worldPosition.z / cellSize) * cellSize;
      return new Vector3(snappedX, worldPosition.y, snappedZ);
    }

    /// <summary>
    /// Snap world position to grid cell center.
    /// </summary>
    public static Vector3 SnapToGridCenter(Vector3 worldPosition) {
      var snappedX = Mathf.Floor(worldPosition.x / cellSize) * cellSize + cellSize * 0.5f;
      var snappedZ = Mathf.Floor(worldPosition.z / cellSize) * cellSize + cellSize * 0.5f;
      return new Vector3(snappedX, worldPosition.y, snappedZ);
    }

    #endregion
  }
}
