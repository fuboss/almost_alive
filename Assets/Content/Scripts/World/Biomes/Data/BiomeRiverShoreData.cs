using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Biomes.Data {
  /// <summary>
  /// River shore interaction settings.
  /// </summary>
  [Serializable]
  public class BiomeRiverShoreData {
    
    [Tooltip("How rivers interact with this biome's terrain")]
    public RiverShoreStyle style = RiverShoreStyle.Natural;

    [Tooltip("Shore slope steepness (0 = cliff, 1 = very gradual beach)")]
    [Range(0f, 1f)]
    public float gradient = 0.5f;

    [Tooltip("Width of the shore transition zone (meters)")]
    [Range(1f, 15f)]
    public float width = 4f;

    [ShowIf("@style == RiverShoreStyle.Rocky")]
    [Tooltip("How jagged/irregular the rocky shore is")]
    [Range(0f, 1f)]
    public float rockyIrregularity = 0.5f;
  }
}
