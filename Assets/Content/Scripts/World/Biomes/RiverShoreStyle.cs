namespace Content.Scripts.World.Biomes {
  /// <summary>
  /// Defines how river shores are shaped when passing through a biome.
  /// </summary>
  public enum RiverShoreStyle {
    /// <summary>Natural smooth transition based on biome gradient setting.</summary>
    Natural,
    
    /// <summary>Soft, gradual beach-like shores (meadows, sandy areas).</summary>
    Soft,
    
    /// <summary>Steep, jagged cliff-like shores (mountains, rocky hills).</summary>
    Rocky,
    
    /// <summary>Very gradual, marshy transition (swamps, wetlands).</summary>
    Marshy,
    
    /// <summary>Terraced steps down to water (man-made or geological).</summary>
    Terraced
  }
}
