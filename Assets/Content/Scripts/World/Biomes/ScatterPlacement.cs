namespace Content.Scripts.World.Biomes {
  /// <summary>
  /// Defines where scatter can be placed based on terrain features.
  /// </summary>
  public enum ScatterPlacement {
    Any,         // Uses slopeRange from rule or override
    FlatOnly,    // 0-15°
    SlopeOnly,   // 15-45°
    CliffOnly,   // 45-90°
    CliffEdge,   // Top of cliff (requires TerrainFeatureMap)
    CliffBase,   // Bottom of cliff (requires TerrainFeatureMap)
    Valley       // Low areas (requires TerrainFeatureMap)
  }
}
