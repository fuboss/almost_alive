using System.Collections.Generic;
using Content.Scripts.World.Vegetation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Biomes {
  /// <summary>
  /// Configuration for a single biome type.
  /// Defines terrain appearance, height profile, textures, and scatter rules.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Biome", fileName = "Biome_")]
  public class BiomeSO : SerializedScriptableObject {
    
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

    [BoxGroup("Identity")]
    [Tooltip("This biome is a water body (lake, pond) - terrain will be carved below water level")]
    public bool isWaterBody = false;

    [BoxGroup("Identity")]
    [ShowIf("isWaterBody")]
    [Tooltip("Depth below water level at biome center (meters)")]
    [Range(0.5f, 15f)]
    public float waterDepth = 3f;

    [BoxGroup("Identity")]
    [ShowIf("isWaterBody")]
    [Tooltip("How gradual the shore slope is (0 = steep, 1 = very gradual)")]
    [Range(0f, 1f)]
    public float shoreGradient = 0.5f;

    // ═══════════════════════════════════════════════════════════════
    // RIVER INTERACTION
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("River Shore")]
    [Tooltip("How rivers interact with this biome's terrain")]
    public RiverShoreStyle riverShoreStyle = RiverShoreStyle.Natural;

    [FoldoutGroup("River Shore")]
    [Tooltip("Shore slope steepness (0 = cliff, 1 = very gradual beach)")]
    [Range(0f, 1f)]
    public float riverShoreGradient = 0.5f;

    [FoldoutGroup("River Shore")]
    [Tooltip("Width of the shore transition zone (meters)")]
    [Range(1f, 15f)]
    public float riverShoreWidth = 4f;

    [FoldoutGroup("River Shore")]
    [ShowIf("@riverShoreStyle == RiverShoreStyle.Rocky")]
    [Tooltip("How jagged/irregular the rocky shore is")]
    [Range(0f, 1f)]
    public float rockyIrregularity = 0.5f;

    // ═══════════════════════════════════════════════════════════════
    // HEIGHT (for TerrainSculptor)
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Height")]
    [Tooltip("Base height offset for this biome (0 = terrain base level)")]
    [Range(0f, 100f)]
    public float baseHeight = 10f;

    [FoldoutGroup("Height")]
    [Tooltip("Maximum height variation from noise")]
    [Range(0f, 50f)]
    public float heightVariation = 5f;

    [FoldoutGroup("Height")]
    [Tooltip("Noise asset for height variation (optional - uses legacy params if null)")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public Generation.Noise.NoiseSO heightNoise;

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
    // VEGETATION (grass, bushes - Unity Terrain Details)
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Vegetation")]
    [Tooltip("Grass, flowers, and decorative vegetation for this biome")]
    [InlineProperty, HideLabel]
    public BiomeVegetationConfig vegetationConfig = new();

    // preview texture for vegetation mask (editor only)
#if UNITY_EDITOR
    [FoldoutGroup("Vegetation"), PropertyOrder(900)]
    [ShowIf("hasVegetation")]
    [PreviewField(64, ObjectFieldAlignment.Left)]
    public Texture2D vegetationMaskPreview;

    [Button("Generate Vegetation Mask Preview"), FoldoutGroup("Vegetation")]
    private void GenerateVegetationMaskPreview() {
      var t = Terrain.activeTerrain;
      if (t == null) {
        Debug.LogError($"[{name}] No terrain found for mask preview");
        return;
      }

      var terrainData = t.terrainData;
      var detailResolution = terrainData.detailResolution;
      if (detailResolution <= 0) {
        Debug.LogError($"[{name}] Terrain detail resolution is zero");
        return;
      }

      var terrainPos = t.transform.position;
      var terrainSize = terrainData.size;
      var seed = WorldGeneratorConfigSO.GetFromResources()?.Data.seed ?? System.Environment.TickCount;

      var settings = new Content.Scripts.World.Vegetation.Mask.MaskSettings();
      settings.mode = vegetationConfig.maskMode;
      settings.scale = vegetationConfig.maskScale;
      settings.fbmOctaves = vegetationConfig.maskOctaves;
      settings.fbmPersistence = vegetationConfig.maskPersistence;
      settings.threshold = vegetationConfig.maskThreshold;
      settings.blend = vegetationConfig.maskBlend;
      settings.useStochasticCulling = vegetationConfig.maskUseStochastic;
      settings.stochasticBlend = vegetationConfig.maskStochasticBlend;
      settings.cacheEnabled = vegetationConfig.maskCacheEnabled;
      settings.seedOffset = seed;

      var mask = Content.Scripts.World.Vegetation.Mask.MaskService.GetMask(terrainData, detailResolution, terrainPos, terrainSize, seed, settings);

      // create texture from mask (downsampled to 256 max for preview)
      var previewSize = Mathf.Min(256, detailResolution);
      var tex = new Texture2D(previewSize, previewSize, TextureFormat.RGBA32, false);
      for (var y = 0; y < previewSize; y++) {
        for (var x = 0; x < previewSize; x++) {
          var srcX = Mathf.FloorToInt((float)x / previewSize * detailResolution);
          var srcY = Mathf.FloorToInt((float)y / previewSize * detailResolution);
          srcX = Mathf.Clamp(srcX, 0, detailResolution - 1);
          srcY = Mathf.Clamp(srcY, 0, detailResolution - 1);
          var v = mask[srcY, srcX];
          tex.SetPixel(x, y, new Color(v, v, v, 1f));
        }
      }
      tex.Apply();

      // Save temporary asset (overwrite existing)
      var dir = "Assets/Temp";
      if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
      var path = $"{dir}/biome_{name}_mask.png";
      System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
      UnityEditor.AssetDatabase.ImportAsset(path);
      var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
      vegetationMaskPreview = asset;
      UnityEditor.EditorUtility.SetDirty(this);

      // create or update a preview quad in the scene so user can visually inspect mask
      try {
        var previewName = $"[BiomeMaskPreview_{name}]";
        var existing = UnityEngine.GameObject.Find(previewName);
        UnityEngine.GameObject go;
        if (existing == null) {
          go = GameObject.CreatePrimitive(PrimitiveType.Quad);
          go.name = previewName;
          // keep preview separate from generated world container
          UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Biome Mask Preview");
        } else {
          go = existing;
        }

        // position quad above terrain a little
        var center = terrainPos + new Vector3(terrainSize.x * 0.5f, 0.5f, terrainSize.z * 0.5f);
        go.transform.position = center;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(terrainSize.x, terrainSize.z, 1f);

        // create material
        var matPath = $"Assets/Temp/biome_{name}_mask.mat";
        UnityEngine.Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) {
          var shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Unlit/Color");
          mat = new Material(shader);
          UnityEditor.AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.mainTexture = asset;
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = mat;

        // mark scene dirty
        UnityEditor.EditorUtility.SetDirty(go);
      } catch (System.Exception ex) {
        Debug.LogWarning($"[{name}] Failed to create scene preview: {ex.Message}");
      }

      Debug.Log($"[{name}] Vegetation mask preview generated: {path}");
    }
#endif

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
      return baseHeight + heightProfile.Evaluate(normalizedDistance) * heightVariation;
    }

    public bool hasScatters => scatterConfigs != null && scatterConfigs.Count > 0;
    public bool hasVegetation => vegetationConfig?.layers != null && vegetationConfig.layers.Length > 0;

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
