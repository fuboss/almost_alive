using System;
using Content.Scripts.World.Vegetation;
using Sirenix.OdinInspector;

namespace Content.Scripts.World.Biomes.Data {
  /// <summary>
  /// Complete biome configuration data.
  /// Aggregates all biome sub-configurations.
  /// </summary>
  [Serializable]
  public class BiomeData {
    
    [FoldoutGroup("Identity", expanded: true)]
    [HideLabel, InlineProperty]
    public BiomeIdentityData identity = new();

    [FoldoutGroup("Water Body")]
    [HideLabel, InlineProperty]
    public BiomeWaterBodyData waterBody = new();

    [FoldoutGroup("River Shore")]
    [HideLabel, InlineProperty]
    public BiomeRiverShoreData riverShore = new();

    [FoldoutGroup("Height")]
    [HideLabel, InlineProperty]
    public BiomeHeightData height = new();

    [FoldoutGroup("Textures")]
    [HideLabel, InlineProperty]
    public BiomeTextureData textures = new();

    [FoldoutGroup("Vegetation")]
    [HideLabel, InlineProperty]
    public BiomeVegetationConfig vegetation = new();

    // ═══════════════════════════════════════════════════════════════
    // CONVENIENCE ACCESSORS
    // ═══════════════════════════════════════════════════════════════

    public BiomeType Type => identity.type;
    public float Weight => identity.weight;
    public bool IsWaterBody => waterBody.isWaterBody;
  }
}
