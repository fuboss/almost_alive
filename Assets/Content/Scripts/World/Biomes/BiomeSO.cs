using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Biomes {
  /// <summary>
  /// Configuration for a single biome type.
  /// Defines terrain appearance, height profile, textures, and scatter rules.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Biome", fileName = "Biome_")]
  public class BiomeSO : ScriptableObject {
    
    // ═══════════════════════════════════════════════════════════════
    // IDENTITY
    // ═══════════════════════════════════════════════════════════════
    
    [BoxGroup("Identity")]
    public BiomeType type;

    [BoxGroup("Identity")]
    [Tooltip("Color used for debug visualization in Scene view")]
    public Color debugColor = Color.green;

    [BoxGroup("Identity")]
    [Tooltip("Relative weight when distributing biomes (higher = more common)")]
    [Range(0.1f, 10f)]
    public float weight = 1f;

    // ═══════════════════════════════════════════════════════════════
    // HEIGHT (for TerrainSculptor)
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Height")]
    [Tooltip("Base height offset for this biome (0 = terrain base level)")]
    [Range(0f, 100f)]
    public float baseHeight = 10f;

    [FoldoutGroup("Height")]
    [Tooltip("Maximum height variation amplitude")]
    [Range(0f, 50f)]
    public float heightAmplitude = 5f;

    [FoldoutGroup("Height")]
    [Tooltip("Height variation curve (X: distance from center 0-1, Y: height multiplier)")]
    public AnimationCurve heightProfile = AnimationCurve.Linear(0, 1, 1, 0.8f);

    [FoldoutGroup("Height/Noise")]
    [Tooltip("Primary noise frequency (lower = larger features)")]
    [Range(0.001f, 0.1f)]
    public float noiseFrequency = 0.01f;

    [FoldoutGroup("Height/Noise")]
    [Tooltip("Number of noise octaves for detail")]
    [Range(1, 6)]
    public int noiseOctaves = 3;

    [FoldoutGroup("Height/Noise")]
    [Tooltip("How much each octave contributes (persistence)")]
    [Range(0.1f, 0.9f)]
    public float noisePersistence = 0.5f;

    // ═══════════════════════════════════════════════════════════════
    // TEXTURES (for SplatmapPainter)
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Textures")]
    [Tooltip("Primary terrain layer index for this biome (flat areas)")]
    public int primaryTerrainLayer = 0;

    [FoldoutGroup("Textures")]
    [Tooltip("Secondary terrain layer (used on slopes)")]
    public int secondaryTerrainLayer = 1;

    [FoldoutGroup("Textures")]
    [Tooltip("Slope angle (degrees) where secondary layer starts blending")]
    [Range(0f, 60f)]
    public float slopeThreshold = 30f;

    // ═══════════════════════════════════════════════════════════════
    // SCATTER RULES
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Scatter")]
    [Tooltip("Scatter rules for this biome")]
    [AssetsOnly]
    [ListDrawerSettings(ShowFoldout = true)]
    public List<ScatterRuleSO> scatterRules = new();

    // ═══════════════════════════════════════════════════════════════
    // API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Sample height modifier at given normalized distance from biome center.
    /// </summary>
    public float SampleHeightModifier(float normalizedDistance) {
      return baseHeight + heightProfile.Evaluate(normalizedDistance) * heightAmplitude;
    }

    public bool hasScatters => scatterRules != null && scatterRules.Count > 0;
  }
}
