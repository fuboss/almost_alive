using System.Collections.Generic;
using Content.Scripts.Utility;
using Content.Scripts.World.Biomes.Data;
using Content.Scripts.World.Vegetation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Biomes {
  /// <summary>
  /// ScriptableObject container for biome configuration.
  /// Uses ScriptableConfig pattern - actual data in BiomeData class.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Biome", fileName = "Biome_")]
  public class BiomeSO : ScriptableConfig<BiomeData> {
    
    // ═══════════════════════════════════════════════════════════════
    // SCATTER RULES (kept at SO level - references other SOs)
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Scatter")]
    [Tooltip("Scatter configurations for this biome")]
    [ListDrawerSettings(ShowFoldout = true)]
    public List<BiomeScatterConfig> scatterConfigs = new();

    // ═══════════════════════════════════════════════════════════════
    // CONVENIENCE ACCESSORS
    // ═══════════════════════════════════════════════════════════════

    public BiomeType type => Data.identity.type;
    public Color debugColor => Data.identity.debugColor;
    public float weight => Data.identity.weight;
    
    public bool isWaterBody => Data.waterBody.isWaterBody;
    public float waterBodyFloorDepth => Data.waterBody.floorDepth;
    public float waterBodyShoreSteepness => Data.waterBody.shoreSteepness;

    public RiverShoreStyle riverShoreStyle => Data.riverShore.style;
    public float riverShoreGradient => Data.riverShore.gradient;
    public float riverShoreWidth => Data.riverShore.width;
    public float rockyIrregularity => Data.riverShore.rockyIrregularity;

    public float baseHeight => Data.height.baseHeight;
    public float heightVariation => Data.height.heightVariation;
    public float minClearanceAboveWater => Data.height.minClearanceAboveWater;
    public AnimationCurve heightProfile => Data.height.heightProfile;
    
    /// <summary>
    /// Access height data for terrain sculpting.
    /// </summary>
    public BiomeHeightData heightData => Data.height;

    public BiomeVegetationConfig vegetationConfig => Data.vegetation;
    public BiomeTextureData textureData => Data.textures;

    public bool hasScatters => scatterConfigs != null && scatterConfigs.Count > 0;
    public bool hasVegetation => Data.vegetation?.HasVegetation ?? false;

    // ═══════════════════════════════════════════════════════════════
    // TEXTURE LAYER ACCESS
    // ═══════════════════════════════════════════════════════════════

    public int GetBaseLayerIndex() => Data.textures.GetBaseLayerIndex();
    public int GetDetailLayerIndex() => Data.textures.GetDetailLayerIndex();
    public int GetSlopeLayerIndex() => Data.textures.GetSlopeLayerIndex();
    public int GetCliffLayerIndex() => Data.textures.GetCliffLayerIndex();

    public BiomeTextureData.TextureSlot baseTexture => Data.textures.baseTexture;
    public BiomeTextureData.DetailTextureSlot detailTexture => Data.textures.detailTexture;
    public BiomeTextureData.SlopeTextureSlot slopeTexture => Data.textures.slopeTexture;
    public BiomeTextureData.CliffTextureSlot cliffTexture => Data.textures.cliffTexture;

    /// <summary>
    /// Sample height modifier at given normalized distance from biome center.
    /// </summary>
    public float SampleHeightModifier(float normalizedDistance) {
      return Data.height.SampleHeightModifier(normalizedDistance);
    }

    // ═══════════════════════════════════════════════════════════════
    // GLOBAL PALETTE ACCESS (static)
    // ═══════════════════════════════════════════════════════════════

    private static TerrainPaletteSO _cachedPalette;

    public static TerrainPaletteSO GetGlobalPalette() {
      if (_cachedPalette == null) {
        var config = WorldGeneratorConfigSO.GetFromResources();
        _cachedPalette = config != null ? config.Data.terrainPalette : null;
      }
      return _cachedPalette;
    }

    public static void ClearPaletteCache() {
      _cachedPalette = null;
    }

    public static IEnumerable<string> GetGlobalPaletteNames() {
      var palette = GetGlobalPalette();
      if (palette == null) {
        yield return "(no palette in config)";
        yield break;
      }

      yield return ""; // empty option
      foreach (var entry in palette.layers) {
        if (!string.IsNullOrEmpty(entry.name)) {
          yield return entry.name;
        }
      }
    }

    public static Texture2D GetLayerPreview(string layerName) {
      if (string.IsNullOrEmpty(layerName)) return null;

      var palette = GetGlobalPalette();
      if (palette == null) return null;

      foreach (var entry in palette.layers) {
        if (entry.name == layerName && entry.layer != null) {
          return entry.layer.diffuseTexture as Texture2D;
        }
      }
      return null;
    }

    // ═══════════════════════════════════════════════════════════════
    // HEIGHT VALIDATION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Calculate max possible height for this biome.
    /// </summary>
    public float GetMaxHeight() => baseHeight + heightVariation;
    
    /// <summary>
    /// Calculate min possible height for this biome.
    /// </summary>
    public float GetMinHeight() => baseHeight - heightVariation;

    /// <summary>
    /// Check if this biome's heights are within safe range for terrain generation.
    /// Returns validation result with warnings/errors.
    /// </summary>
    public BiomeHeightValidation ValidateHeights(float terrainSize, float blendDistance, float waterLevel) {
      var result = new BiomeHeightValidation { biomeName = name };
      
      // Rule: max recommended height step per blend distance
      // For smooth transitions, height change should be ~1m per 10m of blend
      float maxRecommendedHeightPerBlend = blendDistance * 0.1f;
      
      // Rule: total height range should be reasonable relative to terrain size
      float maxRecommendedTotalRange = terrainSize * 0.05f; // 5% of terrain size
      
      result.maxHeight = GetMaxHeight();
      result.minHeight = GetMinHeight();
      result.heightRange = result.maxHeight - result.minHeight;
      result.recommendedMaxStep = maxRecommendedHeightPerBlend;
      
      // Water body validation
      if (isWaterBody) {
        if (baseHeight > waterLevel) {
          result.AddError($"Water body base height ({baseHeight:F1}m) should be at or below water level ({waterLevel:F1}m)");
        }
      } else {
        // Land biome: check minimum clearance
        if (GetMinHeight() < waterLevel + minClearanceAboveWater) {
          result.AddWarning($"Min height ({GetMinHeight():F1}m) might go below water+clearance ({waterLevel + minClearanceAboveWater:F1}m)");
        }
      }
      
      return result;
    }

    /// <summary>
    /// Validate height difference between this and another biome.
    /// </summary>
    public static BiomeTransitionValidation ValidateTransition(BiomeSO from, BiomeSO to, float blendDistance) {
      var result = new BiomeTransitionValidation {
        fromBiome = from.name,
        toBiome = to.name,
        blendDistance = blendDistance
      };
      
      // Max height difference in worst case
      float maxDiff = Mathf.Abs(from.GetMaxHeight() - to.GetMinHeight());
      float minDiff = Mathf.Abs(from.GetMinHeight() - to.GetMaxHeight());
      result.worstCaseHeightDiff = Mathf.Max(maxDiff, minDiff);
      
      // Slope calculation: height change over blend distance
      result.resultingSlope = Mathf.Atan2(result.worstCaseHeightDiff, blendDistance) * Mathf.Rad2Deg;
      
      // Thresholds
      float maxRecommendedSlope = 25f; // NavMesh friendly
      float maxAllowedSlope = 40f; // Still walkable with difficulty
      
      if (result.resultingSlope > maxAllowedSlope) {
        result.AddError($"Transition creates {result.resultingSlope:F1}° slope (max {maxAllowedSlope}°) - will create cliffs!");
      } else if (result.resultingSlope > maxRecommendedSlope) {
        result.AddWarning($"Transition creates {result.resultingSlope:F1}° slope (recommended <{maxRecommendedSlope}°) - may affect NavMesh");
      }
      
      return result;
    }

    // ═══════════════════════════════════════════════════════════════
    // DEBUG
    // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    [Button("Validate"), FoldoutGroup("Debug")]
    private void Validate() {
      var errors = 0;
      var palette = GetGlobalPalette();

      if (palette == null) {
        Debug.LogError($"[{name}] No terrain palette in WorldGeneratorConfig");
        errors++;
      } else {
        if (GetBaseLayerIndex() < 0) {
          Debug.LogError($"[{name}] Base layer '{baseTexture.layerName}' not in palette");
          errors++;
        }
        if (detailTexture.isEnabled && GetDetailLayerIndex() < 0) {
          Debug.LogError($"[{name}] Detail layer '{detailTexture.layerName}' not in palette");
          errors++;
        }
        if (slopeTexture.isEnabled && GetSlopeLayerIndex() < 0) {
          Debug.LogError($"[{name}] Slope layer '{slopeTexture.layerName}' not in palette");
          errors++;
        }
        if (cliffTexture.isEnabled && GetCliffLayerIndex() < 0) {
          Debug.LogError($"[{name}] Cliff layer '{cliffTexture.layerName}' not in palette");
          errors++;
        }
      }

      if (scatterConfigs != null) {
        for (var i = 0; i < scatterConfigs.Count; i++) {
          var config = scatterConfigs[i];
          if (config == null) {
            Debug.LogError($"[{name}] Null scatter config at index {i}");
            errors++;
          } else if (config.rule == null) {
            Debug.LogError($"[{name}] Null rule in scatter config at index {i}");
            errors++;
          }
        }
      }

      if (errors == 0) {
        Debug.Log($"[{name}] ✓ Biome valid");
      }
    }

    [Button("Initialize Default Vegetation"), FoldoutGroup("Debug")]
    private void InitializeDefaultVegetation() {
      Data.vegetation.InitializeDefaults();
      UnityEditor.EditorUtility.SetDirty(this);
      Debug.Log($"[{name}] Vegetation categories initialized");
    }

    [Button("Clear Palette Cache"), FoldoutGroup("Debug")]
    private void ClearCache() {
      ClearPaletteCache();
      Debug.Log("[BiomeSO] Palette cache cleared");
    }
#endif
  }
}
