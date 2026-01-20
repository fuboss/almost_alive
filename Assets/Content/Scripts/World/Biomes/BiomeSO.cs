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
    // 4 fixed slots: base, detail, slope, cliff
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Textures")]
    [TitleGroup("Textures/Base Layer")]
    [HideLabel, InlineProperty]
    public TextureSlot baseTexture = new() { layerName = "Grass" };

    [FoldoutGroup("Textures")]
    [TitleGroup("Textures/Detail Layer")]
    [InfoBox("Blended with base layer using noise. Leave empty to disable.")]
    [HideLabel, InlineProperty]
    public DetailTextureSlot detailTexture = new();

    [FoldoutGroup("Textures")]
    [TitleGroup("Textures/Slope Layer")]
    [InfoBox("Applied on slopes between slopeRange.x and slopeRange.y degrees.")]
    [HideLabel, InlineProperty]
    public SlopeTextureSlot slopeTexture = new() { layerName = "Rock", slopeRange = new Vector2(25f, 45f) };

    [FoldoutGroup("Textures")]
    [TitleGroup("Textures/Cliff Layer")]
    [InfoBox("Applied on steep cliffs above threshold.")]
    [HideLabel, InlineProperty]
    public CliffTextureSlot cliffTexture = new() { layerName = "Cliff", threshold = 55f };

    // ═══════════════════════════════════════════════════════════════
    // SCATTER RULES
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Scatter")]
    [Tooltip("Scatter configurations for this biome")]
    [ListDrawerSettings(ShowFoldout = true)]
    public List<BiomeScatterConfig> scatterConfigs = new();

    // ═══════════════════════════════════════════════════════════════
    // TEXTURE SLOT CLASSES
    // ═══════════════════════════════════════════════════════════════

    [System.Serializable]
    public class TextureSlot {
      [HorizontalGroup("Row"), LabelWidth(80)]
      [ValueDropdown("@BiomeSO.GetGlobalPaletteNames()")]
      public string layerName;

      [HorizontalGroup("Row", Width = 50)]
      [ShowInInspector, ReadOnly, PreviewField(45, ObjectFieldAlignment.Right), HideLabel]
      public Texture2D preview => BiomeSO.GetLayerPreview(layerName);
    }

    [System.Serializable]
    public class DetailTextureSlot {
      [HorizontalGroup("Row"), LabelWidth(80)]
      [ValueDropdown("@BiomeSO.GetGlobalPaletteNames()")]
      public string layerName;

      [HorizontalGroup("Row", Width = 50)]
      [ShowInInspector, ReadOnly, PreviewField(45, ObjectFieldAlignment.Right), HideLabel]
      public Texture2D preview => BiomeSO.GetLayerPreview(layerName);

      [Range(0f, 1f)]
      [Tooltip("How much detail layer blends with base (0 = none, 1 = full)")]
      public float strength = 0.3f;

      [Range(0.001f, 0.1f)]
      [Tooltip("Noise scale for detail blending (smaller = larger patches)")]
      public float noiseScale = 0.02f;

      public bool isEnabled => !string.IsNullOrEmpty(layerName) && strength > 0f;
    }

    [System.Serializable]
    public class SlopeTextureSlot {
      [HorizontalGroup("Row"), LabelWidth(80)]
      [ValueDropdown("@BiomeSO.GetGlobalPaletteNames()")]
      public string layerName;

      [HorizontalGroup("Row", Width = 50)]
      [ShowInInspector, ReadOnly, PreviewField(45, ObjectFieldAlignment.Right), HideLabel]
      public Texture2D preview => BiomeSO.GetLayerPreview(layerName);

      [MinMaxSlider(0f, 90f, true)]
      [Tooltip("Slope angle range where this texture appears (degrees)")]
      public Vector2 slopeRange = new(25f, 45f);

      public bool isEnabled => !string.IsNullOrEmpty(layerName);
    }

    [System.Serializable]
    public class CliffTextureSlot {
      [HorizontalGroup("Row"), LabelWidth(80)]
      [ValueDropdown("@BiomeSO.GetGlobalPaletteNames()")]
      public string layerName;

      [HorizontalGroup("Row", Width = 50)]
      [ShowInInspector, ReadOnly, PreviewField(45, ObjectFieldAlignment.Right), HideLabel]
      public Texture2D preview => BiomeSO.GetLayerPreview(layerName);

      [Range(0f, 90f)]
      [Tooltip("Slope angle above which cliff texture is applied")]
      public float threshold = 55f;

      public bool isEnabled => !string.IsNullOrEmpty(layerName);
    }

    // ═══════════════════════════════════════════════════════════════
    // GLOBAL PALETTE ACCESS
    // ═══════════════════════════════════════════════════════════════

    private static TerrainPaletteSO _cachedPalette;

    public static TerrainPaletteSO GetGlobalPalette() {
      if (_cachedPalette == null) {
        var config = WorldGeneratorConfigSO.GetFromResources();
        _cachedPalette = config != null ? config.terrainPalette : null;
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
    // API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get layer index from global palette. Returns -1 if not found.
    /// </summary>
    public int GetLayerIndex(string layerName) {
      var palette = GetGlobalPalette();
      return palette != null ? palette.GetIndex(layerName) : -1;
    }

    public int GetBaseLayerIndex() => GetLayerIndex(baseTexture.layerName);
    public int GetDetailLayerIndex() => GetLayerIndex(detailTexture.layerName);
    public int GetSlopeLayerIndex() => GetLayerIndex(slopeTexture.layerName);
    public int GetCliffLayerIndex() => GetLayerIndex(cliffTexture.layerName);

    /// <summary>
    /// Sample height modifier at given normalized distance from biome center.
    /// </summary>
    public float SampleHeightModifier(float normalizedDistance) {
      return baseHeight + heightProfile.Evaluate(normalizedDistance) * heightAmplitude;
    }

    public bool hasScatters => scatterConfigs != null && scatterConfigs.Count > 0;

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

    [Button("Clear Palette Cache"), FoldoutGroup("Debug")]
    private void ClearCache() {
      ClearPaletteCache();
      Debug.Log("[BiomeSO] Palette cache cleared");
    }
#endif
  }
}
