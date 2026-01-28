using Unity.Mathematics;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise.Samplers {
  /// <summary>
  /// Classic Perlin noise using Unity.Mathematics.
  /// Smooth gradient noise, good for terrain base shapes.
  /// Output range: [-1, 1] (before normalization)
  /// </summary>
  [CreateAssetMenu(menuName = "World/Noise/Perlin", fileName = "Noise_Perlin")]
  public class PerlinNoiseSO : NoiseSO {
    
    protected override Vector2 NativeRange => new(-1f, 1f);

    protected override float SampleRaw(float x, float y, int seed) {
      // Unity.Mathematics.noise.cnoise is classic Perlin
      // Add seed as offset to get different patterns
      var seedOffset2D = new float2(seed * 0.31f, seed * 0.47f);
      return noise.cnoise(new float2(x, y) + seedOffset2D);
    }

    protected override float SampleRaw3D(float x, float y, float z, int seed) {
      var seedOffset3D = new float3(seed * 0.31f, seed * 0.47f, seed * 0.59f);
      return noise.cnoise(new float3(x, y, z) + seedOffset3D);
    }
  }
}
