using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Biomes.Data {
  /// <summary>
  /// Core identity settings for a biome.
  /// </summary>
  [Serializable]
  public class BiomeIdentityData {
    
    [Tooltip("Biome type identifier")]
    public BiomeType type;

    [Tooltip("Color used for debug visualization in Scene view")]
    public Color debugColor = Color.green;

    [Tooltip("Relative weight when distributing biomes (higher = more common)")]
    [Range(0.1f, 10f)]
    public float weight = 1f;
  }
}
