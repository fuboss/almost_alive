using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise {
  /// <summary>
  /// Inline noise configuration for per-biome use.
  /// Stores settings locally - original NoiseSO template is never modified.
  /// Template is only used for "Copy From" operation.
  /// </summary>
  [Serializable]
  public class BiomeNoiseConfig : INoiseSampler {
    
    // ═══════════════════════════════════════════════════════════════
    // TEMPLATE (read-only source for copying)
    // ═══════════════════════════════════════════════════════════════

    [TitleGroup("Template")]
    [Tooltip("Optional template to copy settings from. Original asset is NEVER modified.")]
    [HorizontalGroup("Template/Row")]
    [HideLabel]
    public NoiseSO template;

    [HorizontalGroup("Template/Row", Width = 80)]
    [Button("Copy"), EnableIf("@template != null")]
    private void CopyFromTemplate() {
      if (template == null) return;
      
      frequency = template.frequency;
      amplitude = template.amplitude;
      offset = template.offset;
      seedOffset = template.seedOffset;
      normalize = template.normalize;
      invert = template.invert;
      power = template.power;
      
      _previewDirty = true;
      Debug.Log($"[BiomeNoiseConfig] Copied settings from '{template.name}'");
    }

    // ═══════════════════════════════════════════════════════════════
    // LOCAL SETTINGS (these are what actually gets used)
    // ═══════════════════════════════════════════════════════════════

    [TitleGroup("Settings")]
    [Tooltip("Scale of noise features. Lower = larger features")]
    [Range(0.001f, 0.5f)]
    [OnValueChanged("MarkPreviewDirty")]
    public float frequency = 0.02f;

    [TitleGroup("Settings")]
    [Tooltip("Output multiplier (height contribution)")]
    [Range(0f, 2f)]
    [OnValueChanged("MarkPreviewDirty")]
    public float amplitude = 1f;

    [TitleGroup("Settings")]
    [Tooltip("Shift noise pattern")]
    [OnValueChanged("MarkPreviewDirty")]
    public Vector2 offset = Vector2.zero;

    [TitleGroup("Settings")]
    [Tooltip("Additional seed offset for this biome")]
    [OnValueChanged("MarkPreviewDirty")]
    public int seedOffset = 0;

    [FoldoutGroup("Output Processing")]
    [Tooltip("Remap output to 0-1 range")]
    [OnValueChanged("MarkPreviewDirty")]
    public bool normalize = true;

    [FoldoutGroup("Output Processing")]
    [Tooltip("Invert output (1 - value)")]
    [OnValueChanged("MarkPreviewDirty")]
    public bool invert = false;

    [FoldoutGroup("Output Processing")]
    [Tooltip("Apply power curve to output")]
    [Range(0.1f, 5f)]
    [OnValueChanged("MarkPreviewDirty")]
    public float power = 1f;

    // ═══════════════════════════════════════════════════════════════
    // FBM SETTINGS
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("FBM (Fractal)")]
    [Tooltip("Enable Fractal Brownian Motion for more natural terrain")]
    [OnValueChanged("MarkPreviewDirty")]
    public bool useFBM = true;

    [FoldoutGroup("FBM (Fractal)")]
    [ShowIf("useFBM")]
    [Tooltip("Number of noise layers to stack")]
    [Range(1, 8)]
    [OnValueChanged("MarkPreviewDirty")]
    public int octaves = 4;

    [FoldoutGroup("FBM (Fractal)")]
    [ShowIf("useFBM")]
    [Tooltip("How much each octave contributes (0.5 = each layer half strength)")]
    [Range(0.1f, 0.9f)]
    [OnValueChanged("MarkPreviewDirty")]
    public float persistence = 0.5f;

    [FoldoutGroup("FBM (Fractal)")]
    [ShowIf("useFBM")]
    [Tooltip("Frequency multiplier per octave")]
    [Range(1.5f, 3f)]
    [OnValueChanged("MarkPreviewDirty")]
    public float lacunarity = 2f;

    // ═══════════════════════════════════════════════════════════════
    // PREVIEW
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Preview", expanded: true)]
    [ShowInInspector, ReadOnly]
    [PreviewField(180, ObjectFieldAlignment.Center)]
    [HideLabel]
    public Texture2D Preview => GetOrGeneratePreview();

    [FoldoutGroup("Preview")]
    [Range(64, 256)]
    [OnValueChanged("MarkPreviewDirty")]
    public int previewSize = 128;

    [FoldoutGroup("Preview")]
    [Tooltip("World size represented by preview (affects visible detail)")]
    [Range(50f, 500f)]
    [OnValueChanged("MarkPreviewDirty")]
    public float previewWorldSize = 200f;

    [FoldoutGroup("Preview")]
    [HorizontalGroup("Preview/Buttons")]
    [Button("Regenerate", ButtonSizes.Small)]
    private void RegeneratePreview() {
      _previewDirty = true;
      GetOrGeneratePreview();
    }

    [HorizontalGroup("Preview/Buttons")]
    [Button("Clear Cache", ButtonSizes.Small)]
    private void ClearPreviewCache() {
      if (_cachedPreview != null) {
        UnityEngine.Object.DestroyImmediate(_cachedPreview);
        _cachedPreview = null;
      }
      _previewDirty = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // PREVIEW GENERATION
    // ═══════════════════════════════════════════════════════════════

    [NonSerialized] private Texture2D _cachedPreview;
    [NonSerialized] private int _cachedHash;
    [NonSerialized] private bool _previewDirty = true;

    private void MarkPreviewDirty() {
      _previewDirty = true;
    }

    private Texture2D GetOrGeneratePreview() {
      var hash = GetParameterHash();
      
      if (_cachedPreview == null || _cachedHash != hash || _previewDirty) {
        _cachedPreview = GeneratePreviewTexture(previewSize);
        _cachedHash = hash;
        _previewDirty = false;
      }
      
      return _cachedPreview;
    }

    private Texture2D GeneratePreviewTexture(int size) {
      var tex = new Texture2D(size, size, TextureFormat.RGB24, false) {
        filterMode = FilterMode.Bilinear,
        wrapMode = TextureWrapMode.Clamp
      };

      var pixels = new Color[size * size];
      var previewSeed = seedOffset;

      for (int y = 0; y < size; y++) {
        for (int x = 0; x < size; x++) {
          // Map to world coordinates based on preview size
          var wx = (x / (float)size) * previewWorldSize;
          var wy = (y / (float)size) * previewWorldSize;
          
          var value = SampleWithSeed(wx, wy, previewSeed);
          
          // Visualize: green for positive, blue for negative (if not normalized)
          if (normalize) {
            value = Mathf.Clamp01(value);
            pixels[y * size + x] = new Color(value, value, value);
          } else {
            // Show signed values with color coding
            if (value >= 0) {
              var v = Mathf.Clamp01(value);
              pixels[y * size + x] = new Color(v * 0.3f, v, v * 0.3f); // Greenish
            } else {
              var v = Mathf.Clamp01(-value);
              pixels[y * size + x] = new Color(v * 0.3f, v * 0.3f, v); // Blueish
            }
          }
        }
      }

      tex.SetPixels(pixels);
      tex.Apply();
      return tex;
    }

    private int GetParameterHash() {
      var h1 = HashCode.Combine(frequency, amplitude, offset, seedOffset, normalize, invert, power, previewSize);
      var h2 = HashCode.Combine(useFBM, octaves, persistence, lacunarity, previewWorldSize);
      return HashCode.Combine(h1, h2);
    }

    // ═══════════════════════════════════════════════════════════════
    // RUNTIME
    // ═══════════════════════════════════════════════════════════════

    [NonSerialized] private int _seed;

    public void SetSeed(int seed) {
      _seed = seed;
    }

    public float Sample(float x, float y) {
      return SampleWithSeed(x, y, _seed);
    }

    public float Sample(float x, float y, float z) {
      // 2D noise for height - ignore z
      return SampleWithSeed(x, y, _seed);
    }

    /// <summary>
    /// Sample noise at world position with explicit seed.
    /// </summary>
    public float SampleWithSeed(float x, float y, int seed) {
      var nx = (x + offset.x) * frequency;
      var ny = (y + offset.y) * frequency;
      
      float value;
      
      if (useFBM) {
        value = SampleFBM(nx, ny, seed + seedOffset);
      } else {
        value = SamplePerlin(nx, ny, seed + seedOffset);
      }
      
      return PostProcess(value);
    }

    private float SamplePerlin(float x, float y, int seed) {
      // Offset by seed for variation
      var sx = x + seed * 0.31f;
      var sy = y + seed * 0.47f;
      return Mathf.PerlinNoise(sx, sy) * 2f - 1f; // Remap to [-1, 1]
    }

    private float SampleFBM(float x, float y, int seed) {
      var value = 0f;
      var amp = 1f;
      var freq = 1f;
      var maxValue = 0f;

      for (int i = 0; i < octaves; i++) {
        var sx = x * freq + seed * 0.31f + i * 100f;
        var sy = y * freq + seed * 0.47f + i * 100f;
        
        value += (Mathf.PerlinNoise(sx, sy) * 2f - 1f) * amp;
        maxValue += amp;
        
        amp *= persistence;
        freq *= lacunarity;
      }

      return value / maxValue; // Normalize to [-1, 1]
    }

    private float PostProcess(float value) {
      // Normalize to 0-1
      if (normalize) {
        value = (value + 1f) * 0.5f; // [-1,1] -> [0,1]
      }
      
      // Apply power curve
      if (Mathf.Abs(power - 1f) > 0.001f) {
        value = Mathf.Pow(Mathf.Abs(value), power) * Mathf.Sign(value);
      }
      
      // Invert
      if (invert) {
        value = 1f - value;
      }
      
      // Apply amplitude
      return value * amplitude;
    }

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Check if config has valid settings.
    /// </summary>
    public bool IsValid => frequency > 0f && amplitude > 0f;

    /// <summary>
    /// Create default config for terrain height.
    /// </summary>
    public static BiomeNoiseConfig CreateDefault() {
      return new BiomeNoiseConfig {
        frequency = 0.015f,
        amplitude = 1f,
        useFBM = true,
        octaves = 4,
        persistence = 0.5f,
        lacunarity = 2f,
        normalize = true
      };
    }

    /// <summary>
    /// Create config with values from template.
    /// </summary>
    public static BiomeNoiseConfig CreateFromTemplate(NoiseSO template) {
      if (template == null) return CreateDefault();
      
      return new BiomeNoiseConfig {
        template = template,
        frequency = template.frequency,
        amplitude = template.amplitude,
        offset = template.offset,
        seedOffset = template.seedOffset,
        normalize = template.normalize,
        invert = template.invert,
        power = template.power
      };
    }
  }
}
