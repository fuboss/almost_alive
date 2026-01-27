using UnityEngine;

namespace Content.Scripts.World.Vegetation.Mask {
  /// <summary>
  /// Contract for mask generators (Perlin, Voronoi, etc.).
  /// GenerateMask must always return a non-null float[,] of size [detailResolution, detailResolution].
  /// Settings parameter is treated opaquely by the contract (implementations may cast to MaskSettings).
  /// </summary>
  public interface IMaskGenerator {
    float[,] GenerateMask(TerrainData terrainData, int detailResolution, Vector3 terrainPos, Vector3 terrainSize, int seed, object settings);
  }
}
