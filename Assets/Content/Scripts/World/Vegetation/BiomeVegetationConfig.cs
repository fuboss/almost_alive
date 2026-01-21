using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Vegetation {
  /// <summary>
  /// Configuration for a single vegetation layer within a biome.
  /// Controls density, terrain filters, and falloff curves.
  /// </summary>
  [Serializable]
  public class VegetationLayerConfig {
    [HorizontalGroup("Main", Width = 0.4f), HideLabel]
    [Required]
    public VegetationPrototypeSO prototype;
    
    [HorizontalGroup("Main"), LabelWidth(50)]
    [Tooltip("Base density (0-1, maps to 0-15 in terrain)")]
    [Range(0f, 1f)]
    public float density = 0.5f;
    
    [Range(0.1f, 3f)] [HorizontalGroup("Main")]
    public float coverage;

    [FoldoutGroup("Terrain Filters", expanded: false)]
    [Tooltip("Density multiplier based on slope (X=slope°, Y=multiplier)")]
    public AnimationCurve slopeFalloff = AnimationCurve.Linear(0f, 1f, 45f, 0f);
    
    [FoldoutGroup("Terrain Filters")]
    [Tooltip("Density multiplier based on height")]
    public AnimationCurve heightFalloff = AnimationCurve.Constant(0f, 200f, 1f);
    
    [FoldoutGroup("Terrain Filters")]
    [Tooltip("Only place on these terrain texture layers (empty = all)")]
    public int[] allowedTerrainLayers;

    [FoldoutGroup("Noise")]
    [Tooltip("Add noise variation to density")]
    public bool useNoise = true;
    
    [FoldoutGroup("Noise")]
    [ShowIf("useNoise")]
    [Range(0.001f, 0.1f)]
    public float noiseScale = 0.02f;
    
    [FoldoutGroup("Noise")]
    [ShowIf("useNoise")]
    [Range(0f, 1f)]
    public float noiseStrength = 0.3f;

    /// <summary>
    /// Calculate final density at given terrain conditions.
    /// </summary>
    public float CalculateDensity(float slope, float height, float noiseValue) {
      var result = density;
      
      // Apply slope falloff
      result *= slopeFalloff.Evaluate(slope);
      
      // Apply height falloff
      result *= heightFalloff.Evaluate(height);
      
      // Apply noise
      if (useNoise) {
        result *= Mathf.Lerp(1f - noiseStrength, 1f + noiseStrength, noiseValue);
      }
      
      return Mathf.Clamp01(result);
    }

    /// <summary>
    /// Check if terrain layer is allowed.
    /// </summary>
    public bool IsLayerAllowed(int layerIndex) {
      if (allowedTerrainLayers == null || allowedTerrainLayers.Length == 0)
        return true;
      return Array.IndexOf(allowedTerrainLayers, layerIndex) >= 0;
    }
  }

  /// <summary>
  /// Per-biome vegetation configuration.
  /// Contains multiple vegetation layers (grass, flowers, etc.)
  /// </summary>
  [Serializable]
  public class BiomeVegetationConfig {
    [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
    public VegetationLayerConfig[] layers = Array.Empty<VegetationLayerConfig>();
    
    [Tooltip("Global density multiplier for this biome (1.0 = normal, 2.0 = double)")]
    [Range(0f, 6f)]
    public float densityMultiplier = 1f;
    
    [Tooltip("Maximum density per cell (default 15, can go higher for denser grass)")]
    [Range(8, 32)]
    public int maxDensityPerCell = 15;
  }
}
