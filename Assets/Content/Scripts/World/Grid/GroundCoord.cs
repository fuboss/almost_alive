using System;
using UnityEngine;

namespace Content.Scripts.World.Grid {
  /// <summary>
  /// 2D grid coordinate. Represents a cell in world space.
  /// </summary>
  public readonly struct GroundCoord : IEquatable<GroundCoord> {
    public readonly int x;
    public readonly int z;

    public GroundCoord(int x, int z) {
      this.x = x;
      this.z = z;
    }

    #region Conversion

    public static GroundCoord FromWorld(Vector3 worldPos) {
      var size = WorldGrid.cellSize;
      return new GroundCoord(
        Mathf.FloorToInt(worldPos.x / size),
        Mathf.FloorToInt(worldPos.z / size)
      );
    }

    public static GroundCoord FromWorld(float x, float z) {
      var size = WorldGrid.cellSize;
      return new GroundCoord(
        Mathf.FloorToInt(x / size),
        Mathf.FloorToInt(z / size)
      );
    }

    /// <summary>
    /// Returns world position at cell center (y = 0).
    /// </summary>
    public Vector3 ToWorld() {
      var size = WorldGrid.cellSize;
      return new Vector3(
        (x + 0.5f) * size,
        0f,
        (z + 0.5f) * size
      );
    }

    /// <summary>
    /// Returns world position at cell corner (min point).
    /// </summary>
    public Vector3 ToWorldMin() {
      var size = WorldGrid.cellSize;
      return new Vector3(x * size, 0f, z * size);
    }

    /// <summary>
    /// Returns world bounds for this cell.
    /// </summary>
    public Bounds ToBounds(float height = 100f) {
      var size = WorldGrid.cellSize;
      var center = new Vector3((x + 0.5f) * size, height * 0.5f, (z + 0.5f) * size);
      return new Bounds(center, new Vector3(size, height, size));
    }

    #endregion

    #region Neighbors

    public void GetNeighbors4(Span<GroundCoord> result) {
      if (result.Length < 4) throw new ArgumentException("Span must have at least 4 elements");
      result[0] = new GroundCoord(x, z + 1);     // north
      result[1] = new GroundCoord(x + 1, z);     // east
      result[2] = new GroundCoord(x, z - 1);     // south
      result[3] = new GroundCoord(x - 1, z);     // west
    }

    public void GetNeighbors8(Span<GroundCoord> result) {
      if (result.Length < 8) throw new ArgumentException("Span must have at least 8 elements");
      result[0] = new GroundCoord(x, z + 1);     // N
      result[1] = new GroundCoord(x + 1, z + 1); // NE
      result[2] = new GroundCoord(x + 1, z);     // E
      result[3] = new GroundCoord(x + 1, z - 1); // SE
      result[4] = new GroundCoord(x, z - 1);     // S
      result[5] = new GroundCoord(x - 1, z - 1); // SW
      result[6] = new GroundCoord(x - 1, z);     // W
      result[7] = new GroundCoord(x - 1, z + 1); // NW
    }

    #endregion

    #region Distance

    /// <summary>
    /// Manhattan distance (4-directional movement).
    /// </summary>
    public int ManhattanDistance(GroundCoord other) {
      return Mathf.Abs(x - other.x) + Mathf.Abs(z - other.z);
    }

    /// <summary>
    /// Chebyshev distance (8-directional movement, "king move").
    /// Use this for radius checks.
    /// </summary>
    public int ChebyshevDistance(GroundCoord other) {
      return Mathf.Max(Mathf.Abs(x - other.x), Mathf.Abs(z - other.z));
    }

    #endregion

    #region Operators

    public static GroundCoord operator +(GroundCoord a, GroundCoord b) {
      return new GroundCoord(a.x + b.x, a.z + b.z);
    }

    public static GroundCoord operator -(GroundCoord a, GroundCoord b) {
      return new GroundCoord(a.x - b.x, a.z - b.z);
    }

    public static bool operator ==(GroundCoord a, GroundCoord b) => a.x == b.x && a.z == b.z;
    public static bool operator !=(GroundCoord a, GroundCoord b) => !(a == b);

    #endregion

    #region Equality

    public bool Equals(GroundCoord other) => x == other.x && z == other.z;
    public override bool Equals(object obj) => obj is GroundCoord other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(x, z);

    #endregion

    public override string ToString() => $"({x}, {z})";
  }
}
