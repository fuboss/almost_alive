using UnityEngine;

namespace Content.Scripts.World.Vegetation.Mask {
  public class PerlinMaskGenerator : IMaskGenerator {

    public float[,] GenerateMask(TerrainData terrainData, int detailResolution, Vector3 terrainPos, Vector3 terrainSize, int seed, object settings) {
      var maskSettings = settings as MaskSettings ?? new MaskSettings();

      var mask = new float[detailResolution, detailResolution];
      if (detailResolution <= 0) return mask;

      var scale = Mathf.Max(1e-6f, maskSettings.scale);
      var octaves = maskSettings.fbmOctaves <= 0 ? 1 : maskSettings.fbmOctaves;
      var persistence = Mathf.Clamp01(maskSettings.fbmPersistence);
      var lacunarity = Mathf.Max(1f, maskSettings.fbmLacunarity);
      var seedOffset = maskSettings.seedOffset + seed;

      for (var z = 0; z < detailResolution; z++) {
        for (var x = 0; x < detailResolution; x++) {
          var normalizedX = (float)x / detailResolution;
          var normalizedZ = (float)z / detailResolution;
          var wx = terrainPos.x + normalizedX * terrainSize.x;
          var wz = terrainPos.z + normalizedZ * terrainSize.z;

          var nx = wx * scale + seedOffset;
          var nz = wz * scale + seedOffset;

          float value = 0f;
          float amplitude = 1f;
          float frequency = 1f;
          float max = 0f;

          for (var o = 0; o < octaves; o++) {
            value += amplitude * Mathf.PerlinNoise(nx * frequency, nz * frequency);
            max += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
          }

          value /= max;

          // apply thresholding (soft edges)
          if (maskSettings.threshold > 0f) {
            value = SmoothStep(maskSettings.threshold - maskSettings.blend, maskSettings.threshold + maskSettings.blend, value);
          }

          mask[z, x] = value;
        }
      }

      return mask;
    }

    private static float SmoothStep(float edge0, float edge1, float x) {
      var t = Mathf.InverseLerp(edge0, edge1, x);
      return t * t * (3f - 2f * t);
    }
  }
}
