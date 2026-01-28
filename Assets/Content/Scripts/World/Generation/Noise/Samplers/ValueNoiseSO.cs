using Unity.Mathematics;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise.Samplers {
  /// <summary>
  /// Value noise - blocky, interpolated random values.
  /// Creates a more blocky/retro feel compared to gradient noise.
  /// Good for pixelated terrain, retro aesthetics.
  /// Output range: [0, 1]
  /// </summary>
  [CreateAssetMenu(menuName = "World/Noise/Value", fileName = "Noise_Value")]
  public class ValueNoiseSO : NoiseSO {
    
    protected override Vector2 NativeRange => new(0f, 1f);

    protected override float SampleRaw(float x, float y, int seed) {
      // Grid cell coordinates
      var ix = (int)math.floor(x);
      var iy = (int)math.floor(y);
      
      // Fractional part for interpolation
      var fx = x - ix;
      var fy = y - iy;
      
      // Smooth interpolation curve (smoothstep)
      fx = fx * fx * (3f - 2f * fx);
      fy = fy * fy * (3f - 2f * fy);
      
      // Get random values at corners
      var v00 = Hash2D(ix, iy, seed);
      var v10 = Hash2D(ix + 1, iy, seed);
      var v01 = Hash2D(ix, iy + 1, seed);
      var v11 = Hash2D(ix + 1, iy + 1, seed);
      
      // Bilinear interpolation
      var v0 = math.lerp(v00, v10, fx);
      var v1 = math.lerp(v01, v11, fx);
      
      return math.lerp(v0, v1, fy);
    }

    protected override float SampleRaw3D(float x, float y, float z, int seed) {
      var ix = (int)math.floor(x);
      var iy = (int)math.floor(y);
      var iz = (int)math.floor(z);
      
      var fx = x - ix;
      var fy = y - iy;
      var fz = z - iz;
      
      fx = fx * fx * (3f - 2f * fx);
      fy = fy * fy * (3f - 2f * fy);
      fz = fz * fz * (3f - 2f * fz);
      
      // 8 corners
      var v000 = Hash3D(ix, iy, iz, seed);
      var v100 = Hash3D(ix + 1, iy, iz, seed);
      var v010 = Hash3D(ix, iy + 1, iz, seed);
      var v110 = Hash3D(ix + 1, iy + 1, iz, seed);
      var v001 = Hash3D(ix, iy, iz + 1, seed);
      var v101 = Hash3D(ix + 1, iy, iz + 1, seed);
      var v011 = Hash3D(ix, iy + 1, iz + 1, seed);
      var v111 = Hash3D(ix + 1, iy + 1, iz + 1, seed);
      
      // Trilinear interpolation
      var v00 = math.lerp(v000, v100, fx);
      var v10 = math.lerp(v010, v110, fx);
      var v01 = math.lerp(v001, v101, fx);
      var v11 = math.lerp(v011, v111, fx);
      
      var v0 = math.lerp(v00, v10, fy);
      var v1 = math.lerp(v01, v11, fy);
      
      return math.lerp(v0, v1, fz);
    }

    // Simple hash functions for random values at grid points
    private static float Hash2D(int x, int y, int seed) {
      var n = x + y * 57 + seed * 131;
      n = (n << 13) ^ n;
      return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / (float)0x7fffffff;
    }

    private static float Hash3D(int x, int y, int z, int seed) {
      var n = x + y * 57 + z * 113 + seed * 131;
      n = (n << 13) ^ n;
      return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / (float)0x7fffffff;
    }
  }
}
