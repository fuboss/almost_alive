namespace Content.Scripts.World.Biomes {
  /// <summary>
  /// Types of biomes available for world generation.
  /// Each type has distinct terrain characteristics and visual style.
  /// </summary>
  public enum BiomeType {
    Forest,      // 0 - Dense woods with varied terrain
    Meadow,      // 1 - Open grassland, gentle rolling terrain
    Lake,        // 2 - Water body, terrain below water level
    Desert,      // 3 - Arid sandy terrain with dunes
    GreenHills,  // 4 - Lush rolling hills, moderate elevation
    RockyHills,  // 5 - Rocky mountainous terrain, highest elevation
    
    // Future expansion:
    // Swamp,    // 6 - Wetlands, near water level
    // Tundra,   // 7 - Cold, sparse vegetation
    // Canyon,   // 8 - Deep cuts, terraced walls
  }
}
