using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Biomes.Data {
  /// <summary>
  /// Water body settings for lake/pond biomes.
  /// </summary>
  [Serializable]
  public class BiomeWaterBodyData {
    
    [Tooltip("This biome is a water body (lake, pond) - terrain will be carved below water level")]
    public bool isWaterBody = false;

    [ShowIf("isWaterBody")]
    [Tooltip("Floor depth BELOW water surface at biome center (meters)")]
    [Range(0.5f, 15f)]
    public float floorDepth = 3f;

    [ShowIf("isWaterBody")]
    [Tooltip("Shore steepness (0 = cliff, 1 = gentle beach)")]
    [Range(0f, 1f)]
    public float shoreSteepness = 0.5f;
  }
}
