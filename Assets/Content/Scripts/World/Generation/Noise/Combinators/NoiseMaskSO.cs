using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise.Combinators {
  /// <summary>
  /// Dedicated mask-based noise selector.
  /// Supports threshold-based hard cuts and soft blending.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Noise/Mask Select", fileName = "Noise_MaskSelect")]
  public class NoiseMaskSO : NoiseSO {
    
    [BoxGroup("Sources")]
    [Required("Noise when mask is low")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public NoiseSO lowNoise;

    [BoxGroup("Sources")]
    [Required("Noise when mask is high")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public NoiseSO highNoise;

    [BoxGroup("Mask")]
    [Required("Mask noise (0-1 values)")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public NoiseSO maskNoise;

    [BoxGroup("Mask")]
    [Tooltip("Threshold for hard cut (if hardCut enabled)")]
    [Range(0f, 1f)]
    public float threshold = 0.5f;

    [BoxGroup("Mask")]
    [Tooltip("Use hard threshold instead of smooth blend")]
    public bool hardCut;

    [BoxGroup("Mask")]
    [HideIf("hardCut")]
    [Tooltip("Falloff range around threshold for soft blend")]
    [Range(0f, 0.5f)]
    public float falloff = 0.1f;

    [BoxGroup("Mask")]
    [Tooltip("Invert the mask")]
    public bool invertMask;

    protected override Vector2 NativeRange => new(0f, 1f);

    protected override float SampleRaw(float x, float y, int seed) {
      if (lowNoise == null || highNoise == null || maskNoise == null) return 0f;
      
      lowNoise.SetSeed(seed);
      highNoise.SetSeed(seed);
      maskNoise.SetSeed(seed);
      
      var low = lowNoise.Sample(x, y);
      var high = highNoise.Sample(x, y);
      var mask = Mathf.Clamp01(maskNoise.Sample(x, y));
      
      if (invertMask) mask = 1f - mask;
      
      return ApplyMask(low, high, mask);
    }

    protected override float SampleRaw3D(float x, float y, float z, int seed) {
      if (lowNoise == null || highNoise == null || maskNoise == null) return 0f;
      
      lowNoise.SetSeed(seed);
      highNoise.SetSeed(seed);
      maskNoise.SetSeed(seed);
      
      var low = lowNoise.Sample(x, y, z);
      var high = highNoise.Sample(x, y, z);
      var mask = Mathf.Clamp01(maskNoise.Sample(x, y)); // 2D mask typically
      
      if (invertMask) mask = 1f - mask;
      
      return ApplyMask(low, high, mask);
    }

    private float ApplyMask(float low, float high, float mask) {
      if (hardCut) {
        return mask > threshold ? high : low;
      }
      
      // Soft blend with falloff
      var lowEdge = threshold - falloff;
      var highEdge = threshold + falloff;
      
      if (mask <= lowEdge) return low;
      if (mask >= highEdge) return high;
      
      // Smooth interpolation in falloff zone
      var t = (mask - lowEdge) / (highEdge - lowEdge);
      t = t * t * (3f - 2f * t); // Smoothstep
      
      return Mathf.Lerp(low, high, t);
    }

    protected override int GetParameterHash() {
      return HashCode.Combine(
        base.GetParameterHash(),
        lowNoise?.GetHashCode() ?? 0,
        highNoise?.GetHashCode() ?? 0,
        maskNoise?.GetHashCode() ?? 0,
        threshold, hardCut, falloff, invertMask
      );
    }
  }
}
