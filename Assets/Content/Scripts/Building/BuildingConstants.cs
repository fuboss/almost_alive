using Content.Scripts.World.Grid;

namespace Content.Scripts.Building {
  /// <summary>
  /// Constants for building system.
  /// </summary>
  public static class BuildingConstants {
    public const float WallHeight = 3f;
    public static float CellSize => WorldGrid.cellSize;
  }
}
