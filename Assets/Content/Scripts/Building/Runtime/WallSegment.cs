using Content.Scripts.Building.Data;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Runtime representation of a wall segment.
  /// </summary>
  public class WallSegment {
    public WallSide side;
    public int index;
    public WallSegmentType type;
    public GameObject instance;

    public WallSegment(WallSide side, int index, WallSegmentType type = WallSegmentType.Solid) {
      this.side = side;
      this.index = index;
      this.type = type;
      this.instance = null;
    }

    /// <summary>
    /// Calculate local position for this wall segment.
    /// </summary>
    public Vector3 GetLocalPosition(Vector2Int footprint) {
      var cellSize = BuildingConstants.CellSize;
      var halfWall = BuildingConstants.WallHeight * 0.5f;

      return side switch {
        // North wall: along +Z edge
        WallSide.North => new Vector3(
          (index + 0.5f) * cellSize,
          halfWall,
          footprint.y * cellSize
        ),
        // South wall: along -Z edge (z=0)
        WallSide.South => new Vector3(
          (index + 0.5f) * cellSize,
          halfWall,
          0
        ),
        // East wall: along +X edge
        WallSide.East => new Vector3(
          footprint.x * cellSize,
          halfWall,
          (index + 0.5f) * cellSize
        ),
        // West wall: along -X edge (x=0)
        WallSide.West => new Vector3(
          0,
          halfWall,
          (index + 0.5f) * cellSize
        ),
        _ => Vector3.zero
      };
    }

    /// <summary>
    /// Get rotation for wall segment (facing outward).
    /// </summary>
    public Quaternion GetLocalRotation() {
      return side switch {
        WallSide.North => Quaternion.Euler(0, 0, 0),
        WallSide.South => Quaternion.Euler(0, 180, 0),
        WallSide.East => Quaternion.Euler(0, 90, 0),
        WallSide.West => Quaternion.Euler(0, -90, 0),
        _ => Quaternion.identity
      };
    }

    /// <summary>
    /// Destroy current instance if exists.
    /// </summary>
    public void DestroyInstance() {
      if (instance != null) {
        Object.Destroy(instance);
        instance = null;
      }
    }
  }
}
