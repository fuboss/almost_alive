using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Biomes {
  /// <summary>
  /// Wrapper over ScatterRuleSO with per-biome terrain overrides.
  /// </summary>
  [Serializable]
  public class BiomeScatterConfig {
    [HorizontalGroup("Main", Width = 0.5f), HideLabel]
    [AssetsOnly, Required]
    public ScatterRuleSO rule;

    [HorizontalGroup("Main", Width = 100), LabelWidth(60)]
    public ScatterPlacement placement = ScatterPlacement.Any;

    [FoldoutGroup("Terrain Overrides", expanded: false)]
    public bool overrideSlopeRange;

    [FoldoutGroup("Terrain Overrides")]
    [ShowIf("overrideSlopeRange")]
    [MinMaxSlider(0f, 90f, true)]
    public Vector2 slopeRange = new(0f, 30f);

    [FoldoutGroup("Terrain Overrides")]
    public bool overrideHeightRange;

    [FoldoutGroup("Terrain Overrides")]
    [ShowIf("overrideHeightRange")]
    [MinMaxSlider(-100f, 500f, true)]
    public Vector2 heightRange = new(0f, 100f);

    /// <summary>
    /// Get effective slope range (override or from rule).
    /// </summary>
    public Vector2 GetSlopeRange() {
      if (overrideSlopeRange) return slopeRange;
      if (rule != null) return rule.slopeRange;
      return new Vector2(0f, 90f);
    }

    /// <summary>
    /// Get effective height range (override or from rule).
    /// </summary>
    public Vector2 GetHeightRange() {
      if (overrideHeightRange) return heightRange;
      if (rule != null) return rule.heightRange;
      return new Vector2(-100f, 500f);
    }

    /// <summary>
    /// Get slope range based on placement type.
    /// </summary>
    public Vector2 GetPlacementSlopeRange() {
      return placement switch {
        ScatterPlacement.FlatOnly => new Vector2(0f, 15f),
        ScatterPlacement.SlopeOnly => new Vector2(15f, 45f),
        ScatterPlacement.CliffOnly => new Vector2(45f, 90f),
        _ => GetSlopeRange()
      };
    }

    /// <summary>
    /// Check if placement requires TerrainFeatureMap.
    /// </summary>
    public bool requiresFeatureMap => placement is 
      ScatterPlacement.CliffEdge or 
      ScatterPlacement.CliffBase or 
      ScatterPlacement.Valley;
  }
}
