using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise.Modifiers {
  /// <summary>
  /// Fractal Brownian Motion (FBM) - stacks multiple octaves of noise.
  /// Essential for realistic terrain with detail at multiple scales.
  /// Each octave adds higher frequency detail with lower amplitude.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Noise/FBM (Fractal)", fileName = "Noise_FBM")]
  public class FBMNoiseSO : NoiseSO {
    
    [BoxGroup("Source")]
    [Required("Assign a base noise to layer")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public NoiseSO source;

    [BoxGroup("Fractal")]
    [Tooltip("Number of noise layers (more = more detail, slower)")]
    [Range(1, 8)]
    public int octaves = 4;

    [BoxGroup("Fractal")]
    [Tooltip("Frequency multiplier per octave (2 = double frequency each octave)")]
    [Range(1f, 4f)]
    public float lacunarity = 2f;

    [BoxGroup("Fractal")]
    [Tooltip("Amplitude multiplier per octave (0.5 = half amplitude each octave)")]
    [Range(0f, 1f)]
    public float persistence = 0.5f;

    [BoxGroup("Fractal")]
    [Tooltip("Different seed offset per octave for variation")]
    public bool varyOctaveSeeds = true;

    protected override Vector2 NativeRange => new(0f, 1f);

    protected override float SampleRaw(float x, float y, int seed) {
      if (source == null) return 0f;
      
      float sum = 0f;
      float amp = 1f;
      float freq = 1f;
      float maxAmp = 0f;
      
      for (int i = 0; i < octaves; i++) {
        var octaveSeed = varyOctaveSeeds ? seed + i * 1000 : seed;
        source.SetSeed(octaveSeed);
        
        sum += source.Sample(x * freq, y * freq) * amp;
        maxAmp += amp;
        
        freq *= lacunarity;
        amp *= persistence;
      }
      
      // Normalize to 0-1
      return sum / maxAmp;
    }

    protected override float SampleRaw3D(float x, float y, float z, int seed) {
      if (source == null) return 0f;
      
      float sum = 0f;
      float amp = 1f;
      float freq = 1f;
      float maxAmp = 0f;
      
      for (int i = 0; i < octaves; i++) {
        var octaveSeed = varyOctaveSeeds ? seed + i * 1000 : seed;
        source.SetSeed(octaveSeed);
        
        sum += source.Sample(x * freq, y * freq, z * freq) * amp;
        maxAmp += amp;
        
        freq *= lacunarity;
        amp *= persistence;
      }
      
      return sum / maxAmp;
    }

    protected override int GetParameterHash() {
      var sourceHash = source != null ? source.GetHashCode() : 0;
      return HashCode.Combine(
        base.GetParameterHash(), 
        sourceHash, octaves, lacunarity, persistence, varyOctaveSeeds
      );
    }
  }
}
