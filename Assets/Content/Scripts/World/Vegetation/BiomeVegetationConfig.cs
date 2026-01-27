using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Vegetation {
  /// <summary>
  /// Configuration for a single vegetation layer within a biome.
  /// Controls density, terrain filters, and falloff curves.
  /// </summary>
  [Serializable]
  public class VegetationLayerConfig : ISerializationCallbackReceiver {
    // Header: preview + prototype on a single row
    [HorizontalGroup("Header", Width = 0.16f), HideLabel]
    #pragma warning disable 0414
    [SerializeField]
    [PreviewField(90, ObjectFieldAlignment.Center)]
    [PropertyOrder(-10)]
    private UnityEngine.Object previewCache;
    #pragma warning restore 0414

    [HorizontalGroup("Header", Width = 0.84f), HideLabel]
    [ShowInInspector]
    [Required]
    public VegetationPrototypeSO prototype;

    // Following fields are placed vertically (each on its own row) for readability
    [BoxGroup("Settings", ShowLabel = false)]
    [Tooltip("Base density (0-1, maps to 0-15 in terrain)")]
    [Range(0f, 1f)]
    public float density = 0.5f;

    [BoxGroup("Settings", ShowLabel = false)]
    [Range(0.1f, 3f)]
    public float coverage;

    [BoxGroup("Settings", ShowLabel = false)]
    [Tooltip("Relative frequency multiplier (1 = normal, >1 denser)")]
    [Range(0.1f, 5f)]
    public float weight = 1f;

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

    // ISerializationCallbackReceiver — обновляем previewCache перед сериализацией/отображением
    public void OnBeforeSerialize() {
      if (prototype == null) {
        previewCache = null;
        return;
      }
      if (prototype.prefabs != null && prototype.prefabs.Length > 0 && prototype.prefabs[0] != null) {
        previewCache = prototype.prefabs[0];
        return;
      }
      previewCache = prototype.prefab;
    }
    
    public void OnAfterDeserialize() {
      // no-op, but touch previewCache so compiler doesn't warn it's unused
      var _ = previewCache;
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
    // Increased upper bound to allow much denser grass when desired. Unity stores detail values as ints,
    // common practical upper bound is 255 (byte range) — this gives you room to tune weight and density.
    [Range(8, 255)]
    public int maxDensityPerCell = 64;
  }
}
