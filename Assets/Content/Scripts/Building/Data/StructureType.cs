namespace Content.Scripts.Building.Data {
  /// <summary>
  /// Type of structure defining what components are generated.
  /// </summary>
  public enum StructureType {
    /// <summary>Enclosed structure with walls, roof, supports, entries</summary>
    Enclosed,
    
    /// <summary>Open structure - no walls/roof/supports, just slots and decorations on terrain</summary>
    Open
  }
}
