using System;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise.Samplers {
  /// <summary>
  /// Ridged noise - creates sharp mountain ridges and canyons.
  /// Based on: 1 - abs(noise), then raised to power for sharpness.
  /// Great for mountain ranges, cliff formations.
  /// Output range: [0, 1]
  /// </summary>
  [CreateAssetMenu(menuName = "World/Noise/Ridged", fileName = "Noise_Ridged")]
  public class RidgedNoiseSO : NoiseSO {
    
    [BoxGroup("Ridged")]
    [Tooltip("Sharpness of ridges (higher = sharper peaks)")]
    [Range(0.5f, 4f)]
    public float ridgePower = 2f;

    [BoxGroup("Ridged")]
    [Tooltip("Offset before power (adjusts ridge width)")]
    [Range(0f, 1f)]
    public float ridgeOffset = 0f;

    protected override Vector2 NativeRange => new(0f, 1f);

    protected override float SampleRaw(float x, float y, int seed) {
      var seedOffset2D = new float2(seed * 0.31f, seed * 0.47f);
      var baseNoise = noise.snoise(new float2(x, y) + seedOffset2D);
      
      // Ridged transformation: 1 - |noise|, then power
      var ridge = 1f - math.abs(baseNoise);
      ridge = math.max(0f, ridge - ridgeOffset);
      ridge = math.pow(ridge, ridgePower);
      
      return ridge;
    }

    protected override float SampleRaw3D(float x, float y, float z, int seed) {
      var seedOffset3D = new float3(seed * 0.31f, seed * 0.47f, seed * 0.59f);
      var baseNoise = noise.snoise(new float3(x, y, z) + seedOffset3D);
      
      var ridge = 1f - math.abs(baseNoise);
      ridge = math.max(0f, ridge - ridgeOffset);
      ridge = math.pow(ridge, ridgePower);
      
      return ridge;
    }

    protected override int GetParameterHash() {
      return HashCode.Combine(base.GetParameterHash(), ridgePower, ridgeOffset);
    }
  }
}
