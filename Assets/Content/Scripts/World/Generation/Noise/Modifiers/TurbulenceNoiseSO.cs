using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise.Modifiers {
  /// <summary>
  /// Domain warping / Turbulence - distorts the input coordinates.
  /// Creates swirly, organic patterns by "bending" the noise space.
  /// Great for rivers, natural-looking distortion.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Noise/Turbulence (Warp)", fileName = "Noise_Turbulence")]
  public class TurbulenceNoiseSO : NoiseSO {
    
    [BoxGroup("Source")]
    [Required("Main noise to distort")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public NoiseSO source;

    [BoxGroup("Warp")]
    [Required("Noise used to warp X coordinate")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public NoiseSO warpNoiseX;

    [BoxGroup("Warp")]
    [Required("Noise used to warp Y coordinate")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public NoiseSO warpNoiseY;

    [BoxGroup("Warp")]
    [Tooltip("Strength of the warping effect")]
    [Range(0f, 50f)]
    public float warpStrength = 10f;

    [BoxGroup("Warp")]
    [Tooltip("Apply multiple warp iterations")]
    [Range(1, 4)]
    public int warpIterations = 1;

    protected override Vector2 NativeRange => source?.Sample(0, 0) >= 0 ? new(0f, 1f) : new(-1f, 1f);

    protected override float SampleRaw(float x, float y, int seed) {
      if (source == null) return 0f;
      
      // Use source noise for warp if not specified
      var warpX = warpNoiseX != null ? warpNoiseX : source;
      var warpY = warpNoiseY != null ? warpNoiseY : source;
      
      var wx = x;
      var wy = y;
      
      // Apply warp iterations
      for (int i = 0; i < warpIterations; i++) {
        warpX.SetSeed(seed + i * 100);
        warpY.SetSeed(seed + i * 100 + 50);
        
        var dx = warpX.Sample(wx, wy) * warpStrength;
        var dy = warpY.Sample(wx + 5.2f, wy + 1.3f) * warpStrength; // Offset to decorrelate
        
        wx += dx;
        wy += dy;
      }
      
      source.SetSeed(seed);
      return source.Sample(wx, wy);
    }

    protected override float SampleRaw3D(float x, float y, float z, int seed) {
      if (source == null) return 0f;
      
      var warpX = warpNoiseX != null ? warpNoiseX : source;
      var warpY = warpNoiseY != null ? warpNoiseY : source;
      
      var wx = x;
      var wy = y;
      
      for (int i = 0; i < warpIterations; i++) {
        warpX.SetSeed(seed + i * 100);
        warpY.SetSeed(seed + i * 100 + 50);
        
        var dx = warpX.Sample(wx, wy, z) * warpStrength;
        var dy = warpY.Sample(wx + 5.2f, wy + 1.3f, z) * warpStrength;
        
        wx += dx;
        wy += dy;
      }
      
      source.SetSeed(seed);
      return source.Sample(wx, wy, z);
    }

    protected override int GetParameterHash() {
      return HashCode.Combine(
        base.GetParameterHash(),
        source?.GetHashCode() ?? 0,
        warpNoiseX?.GetHashCode() ?? 0,
        warpNoiseY?.GetHashCode() ?? 0,
        warpStrength, warpIterations
      );
    }
  }
}
