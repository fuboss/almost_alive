using Unity.Mathematics;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise.Samplers {
  /// <summary>
  /// Simplex noise using Unity.Mathematics.
  /// Improved Perlin with less directional artifacts.
  /// Good for general purpose, slightly faster than Perlin.
  /// Output range: [-1, 1] (before normalization)
  /// </summary>
  [CreateAssetMenu(menuName = "World/Noise/Simplex", fileName = "Noise_Simplex")]
  public class SimplexNoiseSO : NoiseSO {
    
    protected override Vector2 NativeRange => new(-1f, 1f);

    protected override float SampleRaw(float x, float y, int seed) {
      // Unity.Mathematics.noise.snoise is Simplex
      var seedOffset2D = new float2(seed * 0.31f, seed * 0.47f);
      return noise.snoise(new float2(x, y) + seedOffset2D);
    }

    protected override float SampleRaw3D(float x, float y, float z, int seed) {
      var seedOffset3D = new float3(seed * 0.31f, seed * 0.47f, seed * 0.59f);
      return noise.snoise(new float3(x, y, z) + seedOffset3D);
    }
  }
}
