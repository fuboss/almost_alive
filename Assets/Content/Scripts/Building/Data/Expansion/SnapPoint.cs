using System;
using Content.Scripts.Building.Data;
using UnityEngine;

namespace Content.Scripts.Building.Data.Expansion {
  /// <summary>
  /// Defines a point where another structure can attach/expand.
  /// </summary>
  [Serializable]
  public class SnapPoint {
    [Tooltip("Which wall side this snap point is on")]
    public WallSide side;
    
    [Tooltip("Wall segment index (0-based)")]
    public int segmentIndex;
    
    [Tooltip("Required footprint of expansion that can attach here")]
    public Vector2Int expandFootprint = Vector2Int.one;
    
    [Tooltip("Allowed expansion structures (null = any)")]
    public StructureDefinitionSO[] allowedExpansions;

    public SnapPoint() { }

    public SnapPoint(WallSide side, int segmentIndex, Vector2Int footprint) {
      this.side = side;
      this.segmentIndex = segmentIndex;
      this.expandFootprint = footprint;
    }

    /// <summary>
    /// Check if expansion is allowed at this snap point.
    /// </summary>
    public bool IsExpansionAllowed(StructureDefinitionSO expansionDef) {
      if (expansionDef == null) return false;
      
      // Check footprint match
      if (expansionDef.footprint.x > expandFootprint.x && expansionDef.footprint.y > expandFootprint.y) return false;
      
      // Check whitelist (null = any allowed)
      if (allowedExpansions == null || allowedExpansions.Length == 0) return true;
      
      foreach (var allowed in allowedExpansions) {
        if (allowed == expansionDef) return true;
      }
      
      return false;
    }
  }
}
