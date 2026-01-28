using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise {
  /// <summary>
  /// Base ScriptableObject for all noise types.
  /// Provides common parameters, preview generation, and template method pattern.
  /// </summary>
  public abstract class NoiseSO : ScriptableObject, INoiseSampler {
    
    // ═══════════════════════════════════════════════════════════════
    // PARAMETERS
    // ═══════════════════════════════════════════════════════════════

    [BoxGroup("Base")]
    [Tooltip("Scale of noise features. Lower = larger features")]
    [Range(0.001f, 1f)]
    public float frequency = 0.05f;

    [BoxGroup("Base")]
    [Tooltip("Output multiplier")]
    [Range(0f, 2f)]
    public float amplitude = 1f;

    [BoxGroup("Base")]
    [Tooltip("Shift noise pattern")]
    public Vector2 offset;

    [BoxGroup("Base")]
    [Tooltip("Seed offset for variation")]
    public int seedOffset;

    [BoxGroup("Output")]
    [Tooltip("Remap output to 0-1 range")]
    public bool normalize = true;

    [BoxGroup("Output")]
    [Tooltip("Invert output (1 - value)")]
    public bool invert;

    [BoxGroup("Output")]
    [Tooltip("Apply power curve to output")]
    [Range(0.1f, 5f)]
    public float power = 1f;

    // ═══════════════════════════════════════════════════════════════
    // PREVIEW
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Preview")]
    [ShowInInspector, ReadOnly]
    [PreviewField(256, ObjectFieldAlignment.Center)]
    [OnInspectorGUI("OnPreviewGUI")]
    public Texture2D Preview => GetOrGeneratePreview();

    [FoldoutGroup("Preview")]
    [Range(64, 512)]
    public int previewSize = 256;

    [FoldoutGroup("Preview")]
    [Button("Regenerate Preview")]
    private void RegeneratePreview() {
      _previewDirty = true;
      GetOrGeneratePreview();
    }

    // ═══════════════════════════════════════════════════════════════
    // ABSTRACT
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Raw noise sampling without transforms. Override in derived classes.
    /// Should return value in native range (e.g. [-1,1] for Perlin).
    /// </summary>
    protected abstract float SampleRaw(float x, float y, int seed);
    
    /// <summary>
    /// Raw 3D noise sampling. Override in derived classes.
    /// </summary>
    protected abstract float SampleRaw3D(float x, float y, float z, int seed);

    /// <summary>
    /// Native output range of this noise type.
    /// Used for normalization.
    /// </summary>
    protected abstract Vector2 NativeRange { get; }

    // ═══════════════════════════════════════════════════════════════
    // INoiseSampler IMPLEMENTATION
    // ═══════════════════════════════════════════════════════════════

    [NonSerialized] private int _seed;

    public void SetSeed(int seed) {
      _seed = seed;
    }

    public float Sample(float x, float y) {
      // Apply transforms
      var nx = (x + offset.x) * frequency;
      var ny = (y + offset.y) * frequency;
      
      // Sample raw noise
      var value = SampleRaw(nx, ny, _seed + seedOffset);
      
      // Post-process
      return PostProcess(value);
    }

    public float Sample(float x, float y, float z) {
      var nx = (x + offset.x) * frequency;
      var ny = (y + offset.y) * frequency;
      var nz = z * frequency;
      
      var value = SampleRaw3D(nx, ny, nz, _seed + seedOffset);
      return PostProcess(value);
    }

    /// <summary>
    /// Sample at world position with explicit seed.
    /// Useful for editor preview without setting seed.
    /// </summary>
    public float SampleWithSeed(float x, float y, int seed) {
      var nx = (x + offset.x) * frequency;
      var ny = (y + offset.y) * frequency;
      var value = SampleRaw(nx, ny, seed + seedOffset);
      return PostProcess(value);
    }

    private float PostProcess(float value) {
      // Normalize to 0-1
      if (normalize) {
        value = Mathf.InverseLerp(NativeRange.x, NativeRange.y, value);
      }
      
      // Apply power curve
      if (Mathf.Abs(power - 1f) > 0.001f) {
        value = Mathf.Pow(Mathf.Clamp01(value), power);
      }
      
      // Invert
      if (invert) {
        value = 1f - value;
      }
      
      // Apply amplitude
      return value * amplitude;
    }

    // ═══════════════════════════════════════════════════════════════
    // PREVIEW GENERATION
    // ═══════════════════════════════════════════════════════════════

    [NonSerialized] private Texture2D _cachedPreview;
    [NonSerialized] private int _cachedHash;
    [NonSerialized] private bool _previewDirty;

    protected Texture2D GetOrGeneratePreview() {
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
      var previewSeed = seedOffset; // Use seedOffset for preview consistency

      for (int y = 0; y < size; y++) {
        for (int x = 0; x < size; x++) {
          // Map to reasonable world coordinates for preview
          var wx = x / (float)size * 100f;
          var wy = y / (float)size * 100f;
          
          var value = SampleWithSeed(wx, wy, previewSeed);
          value = Mathf.Clamp01(value);
          
          pixels[y * size + x] = new Color(value, value, value);
        }
      }

      tex.SetPixels(pixels);
      tex.Apply();
      return tex;
    }

    protected virtual int GetParameterHash() {
      return HashCode.Combine(
        frequency, amplitude, offset, seedOffset,
        normalize, invert, power, previewSize
      );
    }

    private void OnPreviewGUI() {
      // Mark dirty on parameter change for live preview
      if (GUI.changed) {
        _previewDirty = true;
      }
    }

    private void OnValidate() {
      _previewDirty = true;
    }
  }
}
