using System;
using Content.Scripts.World.Generation.Noise;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Biomes.Data {
  /// <summary>
  /// Height and noise settings for terrain sculpting.
  /// Uses inline BiomeNoiseConfig - original NoiseSO templates are never modified.
  /// </summary>
  [Serializable]
  public class BiomeHeightData {
    
    [TitleGroup("Height")]
    [Tooltip("Base height offset for this biome (0 = terrain base level)")]
    [Range(0f, 100f)]
    public float baseHeight = 10f;

    [TitleGroup("Height")]
    [Tooltip("Maximum height variation from noise")]
    [Range(0f, 50f)]
    public float heightVariation = 5f;

    [TitleGroup("Height")]
    [Tooltip("Minimum height ABOVE water level (prevents submersion)")]
    [Range(0.1f, 5f)]
    public float minClearanceAboveWater = 0.5f;

    [TitleGroup("Height")]
    [Tooltip("Height variation curve (X: distance from center 0-1, Y: height multiplier)")]
    public AnimationCurve heightProfile = AnimationCurve.Linear(0, 1, 1, 0.8f);

    [FoldoutGroup("Height Noise", expanded: true)]
    [Tooltip("Noise configuration for height variation. Settings are stored locally - template is read-only.")]
    [HideLabel, InlineProperty]
    public BiomeNoiseConfig noise = BiomeNoiseConfig.CreateDefault();

    // ═══════════════════════════════════════════════════════════════
    // API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Sample height modifier at given normalized distance from biome center.
    /// Does NOT include noise - just base height + profile curve.
    /// </summary>
    public float SampleHeightModifier(float normalizedDistance) {
      return baseHeight + heightProfile.Evaluate(normalizedDistance) * heightVariation;
    }

    /// <summary>
    /// Sample height noise at world position.
    /// Returns value in range [0, amplitude] if normalized.
    /// </summary>
    public float SampleNoise(float worldX, float worldZ, int seed) {
      if (noise == null || !noise.IsValid) return 0f;
      return noise.SampleWithSeed(worldX, worldZ, seed);
    }

    /// <summary>
    /// Get full height at position: base + profile + noise.
    /// </summary>
    public float GetHeight(float worldX, float worldZ, float normalizedDistanceFromCenter, int seed) {
      var baseH = SampleHeightModifier(normalizedDistanceFromCenter);
      var noiseH = SampleNoise(worldX, worldZ, seed) * heightVariation;
      return baseH + noiseH;
    }

    /// <summary>
    /// Check if this biome has valid height noise.
    /// </summary>
    public bool HasNoise => noise != null && noise.IsValid;

    /// <summary>
    /// Initialize noise sampler with seed for batch operations.
    /// </summary>
    public void InitializeNoise(int seed) {
      noise?.SetSeed(seed);
    }
  }
}
