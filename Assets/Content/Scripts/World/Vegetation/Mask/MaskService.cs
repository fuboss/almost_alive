using UnityEngine;

namespace Content.Scripts.World.Vegetation.Mask {
  public static class MaskService {
    private static readonly PerlinMaskGenerator s_perlin = new PerlinMaskGenerator();
    // VoronoiMaskGenerator can be added later and selected via settings.mode

    public static float[,] GetMask(TerrainData terrainData, int detailResolution, Vector3 terrainPos, Vector3 terrainSize, int seed, MaskSettings settings) {
      if (settings == null) settings = new MaskSettings();

      if (settings.mode == MaskMode.None) {
        var ones = new float[detailResolution, detailResolution];
        for (var z = 0; z < detailResolution; z++) for (var x = 0; x < detailResolution; x++) ones[z, x] = 1f;
        return ones;
      }

      var key = BuildCacheKey(terrainData.GetInstanceID(), detailResolution, settings, seed);
      if (settings.cacheEnabled && MaskCache.TryGet(key, out var cached)) return cached;

      float[,] mask;
      switch (settings.mode) {
        case MaskMode.Perlin:
          mask = s_perlin.GenerateMask(terrainData, detailResolution, terrainPos, terrainSize, seed, settings);
          break;
        case MaskMode.Voronoi:
          // fallback to perlin for now until Voronoi implemented
          mask = s_perlin.GenerateMask(terrainData, detailResolution, terrainPos, terrainSize, seed, settings);
          break;
        default:
          mask = s_perlin.GenerateMask(terrainData, detailResolution, terrainPos, terrainSize, seed, settings);
          break;
      }

      if (settings.cacheEnabled) MaskCache.Add(key, mask);
      return mask;
    }

    public static void ClearCache() => MaskCache.Clear();

    private static string BuildCacheKey(int terrainId, int detailResolution, MaskSettings settings, int seed) {
      return $"{terrainId}_{detailResolution}_{settings.mode}_{settings.scale}_{settings.fbmOctaves}_{settings.fbmPersistence}_{settings.threshold}_{settings.blend}_{settings.voronoiSites}_{settings.voronoiDownsample}_{settings.voronoiFalloff}_{settings.cacheEnabled}_{seed + settings.seedOffset}";
    }
  }
}
