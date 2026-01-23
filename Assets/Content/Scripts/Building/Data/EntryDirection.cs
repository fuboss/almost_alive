using System;

namespace Content.Scripts.Building.Data {
  /// <summary>
  /// Flags for allowed entry directions on a structure.
  /// </summary>
  [Flags]
  public enum EntryDirection {
    None = 0,
    North = 1 << 0,  // +Z
    South = 1 << 1,  // -Z
    East = 1 << 2,   // +X
    West = 1 << 3,   // -X
    All = North | South | East | West
  }
}
