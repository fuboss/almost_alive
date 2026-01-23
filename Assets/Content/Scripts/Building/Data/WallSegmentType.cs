namespace Content.Scripts.Building.Data {
  /// <summary>
  /// Type of wall segment.
  /// </summary>
  public enum WallSegmentType {
    Solid,      // solid wall, no passage
    Doorway,    // wall with doorway for terrain entry
    Passage,    // open passage for expansion connection
    Window      // wall with window (future)
  }
}
