using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise.Combinators {
  /// <summary>
  /// Combines two noise sources using various blend modes.
  /// Powerful tool for creating complex terrain patterns.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Noise/Composite", fileName = "Noise_Composite")]
  public class CompositeNoiseSO : NoiseSO {
    
    [BoxGroup("Sources")]
    [Required("First noise source")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public NoiseSO noiseA;

    [BoxGroup("Sources")]
    [Required("Second noise source")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public NoiseSO noiseB;

    [BoxGroup("Blend")]
    [Tooltip("How to combine the two noise sources")]
    public NoiseBlendMode blendMode = NoiseBlendMode.Lerp;

    [BoxGroup("Blend")]
    [ShowIf("@blendMode == NoiseBlendMode.Lerp")]
    [Tooltip("Blend factor (0 = all A, 1 = all B)")]
    [Range(0f, 1f)]
    public float blend = 0.5f;

    [BoxGroup("Blend")]
    [ShowIf("@blendMode == NoiseBlendMode.Mask")]
    [Tooltip("Noise used as blend mask")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public NoiseSO maskNoise;

    [BoxGroup("Blend")]
    [ShowIf("@blendMode == NoiseBlendMode.Add || blendMode == NoiseBlendMode.Subtract")]
    [Tooltip("Multiplier for second noise before combining")]
    [Range(0f, 2f)]
    public float bWeight = 1f;

    protected override Vector2 NativeRange => new(0f, 1f);

    protected override float SampleRaw(float x, float y, int seed) {
      if (noiseA == null || noiseB == null) return 0f;
      
      noiseA.SetSeed(seed);
      noiseB.SetSeed(seed);
      
      var a = noiseA.Sample(x, y);
      var b = noiseB.Sample(x, y);
      
      return Combine(a, b, x, y, seed);
    }

    protected override float SampleRaw3D(float x, float y, float z, int seed) {
      if (noiseA == null || noiseB == null) return 0f;
      
      noiseA.SetSeed(seed);
      noiseB.SetSeed(seed);
      
      var a = noiseA.Sample(x, y, z);
      var b = noiseB.Sample(x, y, z);
      
      // For mask mode, sample mask at 2D (most masks are 2D)
      return Combine(a, b, x, y, seed);
    }

    private float Combine(float a, float b, float x, float y, int seed) {
      return blendMode switch {
        NoiseBlendMode.Lerp => Mathf.Lerp(a, b, blend),
        NoiseBlendMode.Add => Mathf.Clamp01(a + b * bWeight),
        NoiseBlendMode.Subtract => Mathf.Clamp01(a - b * bWeight),
        NoiseBlendMode.Multiply => a * b,
        NoiseBlendMode.Min => Mathf.Min(a, b),
        NoiseBlendMode.Max => Mathf.Max(a, b),
        NoiseBlendMode.Screen => 1f - (1f - a) * (1f - b),
        NoiseBlendMode.Overlay => a < 0.5f ? 2f * a * b : 1f - 2f * (1f - a) * (1f - b),
        NoiseBlendMode.Difference => Mathf.Abs(a - b),
        NoiseBlendMode.Mask => CombineMask(a, b, x, y, seed),
        _ => a
      };
    }

    private float CombineMask(float a, float b, float x, float y, int seed) {
      if (maskNoise == null) return Mathf.Lerp(a, b, 0.5f);
      
      maskNoise.SetSeed(seed);
      var mask = Mathf.Clamp01(maskNoise.Sample(x, y));
      
      return a * (1f - mask) + b * mask;
    }

    protected override int GetParameterHash() {
      return HashCode.Combine(
        base.GetParameterHash(),
        noiseA?.GetHashCode() ?? 0,
        noiseB?.GetHashCode() ?? 0,
        blendMode, blend,
        maskNoise?.GetHashCode() ?? 0,
        bWeight
      );
    }
  }
}
