using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Configuration for spawning child actors around parent.
  /// </summary>
  [Serializable]
  public class ChildScatterConfig {
    [Tooltip("Child scatter rule to spawn")]
    [Required]
    public ScatterRuleSO rule;

    [Tooltip("Min/max children per parent")]
    [MinMaxSlider(1, 20, true)]
    public Vector2Int countPerParent = new(2, 5);

    [Tooltip("Minimum distance from parent center")]
    [Range(0.5f, 20f)]
    public float radiusMin = 2f;

    [Tooltip("Maximum distance from parent center")]
    [Range(1f, 50f)]
    public float radiusMax = 8f;

    [Tooltip("Use parent's terrain filters in addition to child's own")]
    public bool inheritTerrainFilter = true;

    [Tooltip("Check spacing only against siblings (not global)")]
    public bool localSpacingOnly = true;
  }
}
