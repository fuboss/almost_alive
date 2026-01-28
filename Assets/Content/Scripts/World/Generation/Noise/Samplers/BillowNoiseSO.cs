using Unity.Mathematics;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise.Samplers {
  /// <summary>
  /// Billow noise - creates puffy, cloud-like shapes.
  /// Based on: abs(noise), creates rolling hills effect.
  /// Good for soft terrain features, cloud-like formations.
  /// Output range: [0, 1]
  /// </summary>
  [CreateAssetMenu(menuName = "World/Noise/Billow", fileName = "Noise_Billow")]
  public class BillowNoiseSO : NoiseSO {
    
    protected override Vector2 NativeRange => new(0f, 1f);

    protected override float SampleRaw(float x, float y, int seed) {
      var seedOffset2D = new float2(seed * 0.31f, seed * 0.47f);
      var baseNoise = noise.snoise(new float2(x, y) + seedOffset2D);
      
      // Billow is simply abs(noise)
      return math.abs(baseNoise);
    }

    protected override float SampleRaw3D(float x, float y, float z, int seed) {
      var seedOffset3D = new float3(seed * 0.31f, seed * 0.47f, seed * 0.59f);
      var baseNoise = noise.snoise(new float3(x, y, z) + seedOffset3D);
      
      return math.abs(baseNoise);
    }
  }
}
