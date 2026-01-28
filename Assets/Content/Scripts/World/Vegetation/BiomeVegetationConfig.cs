using System;
using Content.Scripts.World.Vegetation.Mask;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Vegetation {
  
  /// <summary>
  /// Size category for vegetation - affects noise patterns and placement rules.
  /// </summary>
  public enum VegetationSize {
    Small,   // Ground cover, small grass
    Medium,  // Bushes, flowers, tall grass
    Large    // Trees, large shrubs
  }

  /// <summary>
  /// Noise settings for vegetation placement within a category.
  /// Controls where vegetation appears vs bare ground.
  /// </summary>
  [Serializable]
  public class VegetationNoiseSettings {
    
    [Tooltip("Noise algorithm for placement")]
    public MaskMode mode = MaskMode.Perlin;

    [Tooltip("Noise scale (smaller = larger patches)")]
    [Range(0.001f, 0.2f)]
    public float scale = 0.02f;

    [Tooltip("FBM octaves for detail")]
    [Range(1, 6)]
    public int octaves = 3;

    [Tooltip("FBM persistence")]
    [Range(0.1f, 0.9f)]
    public float persistence = 0.5f;

    [Tooltip("Threshold for vegetation placement (0 = everywhere, 1 = nowhere)")]
    [Range(0f, 1f)]
    public float threshold = 0.4f;

    [Tooltip("Softness of threshold edge")]
    [Range(0f, 0.5f)]
    public float blend = 0.15f;

    [Tooltip("Additional stochastic culling for natural variation")]
    public bool useStochastic = false;

    [ShowIf("useStochastic")]
    [Range(0f, 1f)]
    public float stochasticBlend = 0.3f;

    /// <summary>
    /// Convert to MaskSettings for MaskService.
    /// </summary>
    public MaskSettings ToMaskSettings(int seedOffset = 0) {
      return new MaskSettings {
        mode = mode,
        scale = scale,
        fbmOctaves = octaves,
        fbmPersistence = persistence,
        threshold = threshold,
        blend = blend,
        useStochasticCulling = useStochastic,
        stochasticBlend = stochasticBlend,
        cacheEnabled = true,
        seedOffset = seedOffset
      };
    }

    /// <summary>
    /// Create default settings for a size category.
    /// </summary>
    public static VegetationNoiseSettings CreateDefault(VegetationSize size) {
      return size switch {
        VegetationSize.Small => new VegetationNoiseSettings {
          scale = 0.05f,
          octaves = 2,
          threshold = 0.3f,
          blend = 0.2f
        },
        VegetationSize.Medium => new VegetationNoiseSettings {
          scale = 0.025f,
          octaves = 3,
          threshold = 0.45f,
          blend = 0.15f
        },
        VegetationSize.Large => new VegetationNoiseSettings {
          scale = 0.01f,
          octaves = 4,
          threshold = 0.6f,
          blend = 0.1f
        },
        _ => new VegetationNoiseSettings()
      };
    }
  }

  /// <summary>
  /// A category of vegetation with shared noise settings.
  /// Groups similar-sized plants that share placement patterns.
  /// </summary>
  [Serializable]
  public class VegetationCategory {
    
    [HorizontalGroup("Header")]
    [Tooltip("Display name for this category")]
    public string name = "Ground Cover";

    [HorizontalGroup("Header", Width = 100)]
    [Tooltip("Size category affects default noise and terrain rules")]
    public VegetationSize size = VegetationSize.Small;

    [Tooltip("Enable/disable this entire category")]
    public bool enabled = true;

    [Tooltip("Base density multiplier for all layers in this category")]
    [Range(0f, 2f)]
    public float densityMultiplier = 1f;

    [FoldoutGroup("Placement Noise")]
    [Tooltip("Noise settings controlling where this category appears")]
    [HideLabel, InlineProperty]
    public VegetationNoiseSettings noise = new();

    [FoldoutGroup("Terrain Filters")]
    [Tooltip("Density falloff from biome center to edge (X: 0-1 distance, Y: multiplier)")]
    public AnimationCurve biomeEdgeFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f);

    [FoldoutGroup("Terrain Filters")]
    [Tooltip("Density multiplier based on slope (X: degrees, Y: multiplier)")]
    public AnimationCurve slopeFalloff = AnimationCurve.Linear(0f, 1f, 45f, 0f);

    [FoldoutGroup("Terrain Filters")]
    [Tooltip("Density multiplier based on height (X: meters, Y: multiplier)")]
    public AnimationCurve heightFalloff = AnimationCurve.Constant(0f, 200f, 1f);

    [FoldoutGroup("Layers")]
    [Tooltip("Vegetation layers in this category")]
    [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
    public VegetationLayerConfig[] layers = Array.Empty<VegetationLayerConfig>();

    /// <summary>
    /// Calculate terrain-based density modifier.
    /// </summary>
    public float CalculateTerrainModifier(float slope, float height, float biomeEdgeDistance) {
      var result = 1f;
      result *= slopeFalloff.Evaluate(slope);
      result *= heightFalloff.Evaluate(height);
      result *= biomeEdgeFalloff.Evaluate(biomeEdgeDistance);
      return result * densityMultiplier;
    }

    /// <summary>
    /// Create a default category for given size.
    /// </summary>
    public static VegetationCategory CreateDefault(VegetationSize size) {
      var name = size switch {
        VegetationSize.Small => "Ground Cover",
        VegetationSize.Medium => "Bushes & Flowers",
        VegetationSize.Large => "Trees & Shrubs",
        _ => "Vegetation"
      };

      return new VegetationCategory {
        name = name,
        size = size,
        noise = VegetationNoiseSettings.CreateDefault(size),
        slopeFalloff = size == VegetationSize.Large 
          ? AnimationCurve.Linear(0f, 1f, 30f, 0f)  // Trees avoid steep slopes
          : AnimationCurve.Linear(0f, 1f, 45f, 0f)
      };
    }
  }

  /// <summary>
  /// Configuration for a single vegetation layer within a category.
  /// Controls density, terrain filters, and falloff curves.
  /// </summary>
  [Serializable]
  public class VegetationLayerConfig : ISerializationCallbackReceiver {
    
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

    [BoxGroup("Density", ShowLabel = false)]
    [Tooltip("Base density (0-1, maps to terrain detail value)")]
    [Range(0f, 1f)]
    public float density = 0.5f;

    [BoxGroup("Density", ShowLabel = false)]
    [Tooltip("Relative weight when multiple layers compete (higher = more common)")]
    [Range(0.1f, 5f)]
    public float weight = 1f;

    [FoldoutGroup("Per-Layer Noise", expanded: false)]
    [Tooltip("Add extra noise variation specific to this layer")]
    public bool useLayerNoise = false;
    
    [FoldoutGroup("Per-Layer Noise")]
    [ShowIf("useLayerNoise")]
    [Range(0.001f, 0.1f)]
    public float layerNoiseScale = 0.03f;
    
    [FoldoutGroup("Per-Layer Noise")]
    [ShowIf("useLayerNoise")]
    [Range(0f, 1f)]
    public float layerNoiseStrength = 0.3f;

    [FoldoutGroup("Terrain Overrides", expanded: false)]
    [Tooltip("Override category slope falloff for this layer")]
    public bool overrideSlopeFalloff = false;
    
    [FoldoutGroup("Terrain Overrides")]
    [ShowIf("overrideSlopeFalloff")]
    public AnimationCurve slopeFalloff = AnimationCurve.Linear(0f, 1f, 45f, 0f);

    [FoldoutGroup("Terrain Overrides")]
    [Tooltip("Only place on these terrain texture layers (empty = all)")]
    public int[] allowedTerrainLayers;

    /// <summary>
    /// Calculate final density considering all factors.
    /// </summary>
    public float CalculateDensity(float categoryModifier, float slope, float layerNoiseValue) {
      var result = density * categoryModifier;
      
      // Apply per-layer slope override
      if (overrideSlopeFalloff) {
        result *= slopeFalloff.Evaluate(slope);
      }
      
      // Apply per-layer noise
      if (useLayerNoise) {
        result *= Mathf.Lerp(1f - layerNoiseStrength, 1f + layerNoiseStrength, layerNoiseValue);
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
      var _ = previewCache;
    }
  }

  /// <summary>
  /// Per-biome vegetation configuration.
  /// Contains categorized vegetation layers with per-category noise.
  /// </summary>
  [Serializable]
  public class BiomeVegetationConfig {
    
    [Tooltip("Global density multiplier for all vegetation in this biome")]
    [Range(0f, 3f)]
    public float globalDensity = 1f;
    
    [Tooltip("Maximum density per terrain cell (higher = denser grass)")]
    [Range(8, 255)]
    public int maxDensityPerCell = 64;

    [Tooltip("Vegetation categories (grouped by size/type)")]
    [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, CustomAddFunction = "AddDefaultCategory")]
    public VegetationCategory[] categories = Array.Empty<VegetationCategory>();

    // ═══════════════════════════════════════════════════════════════
    // LEGACY SUPPORT (for migration)
    // ═══════════════════════════════════════════════════════════════

    [HideInInspector]
    [Obsolete("Use categories instead")]
    public VegetationLayerConfig[] layers;

    // ═══════════════════════════════════════════════════════════════
    // API
    // ═══════════════════════════════════════════════════════════════

    public bool HasVegetation => categories != null && categories.Length > 0;

    /// <summary>
    /// Get total layer count across all categories.
    /// </summary>
    public int TotalLayerCount {
      get {
        var count = 0;
        if (categories != null) {
          foreach (var cat in categories) {
            if (cat?.layers != null) count += cat.layers.Length;
          }
        }
        return count;
      }
    }

    /// <summary>
    /// Initialize with default categories.
    /// </summary>
    public void InitializeDefaults() {
      categories = new[] {
        VegetationCategory.CreateDefault(VegetationSize.Small),
        VegetationCategory.CreateDefault(VegetationSize.Medium),
        VegetationCategory.CreateDefault(VegetationSize.Large)
      };
    }

    #if UNITY_EDITOR
    private VegetationCategory AddDefaultCategory() {
      // Find next unused size
      var usedSizes = new System.Collections.Generic.HashSet<VegetationSize>();
      if (categories != null) {
        foreach (var cat in categories) usedSizes.Add(cat.size);
      }

      foreach (VegetationSize size in Enum.GetValues(typeof(VegetationSize))) {
        if (!usedSizes.Contains(size)) {
          return VegetationCategory.CreateDefault(size);
        }
      }
      
      return VegetationCategory.CreateDefault(VegetationSize.Small);
    }
    #endif
  }
}
