using UnityEngine;

namespace Content.Scripts.World.Grid.Presentation {
  /// <summary>
  /// Interface for visualizing multi-cell structure footprint during placement.
  /// </summary>
  public interface IFootprintVisualizer {
    /// <summary>
    /// Show structure footprint preview.
    /// </summary>
    /// <param name="origin">Bottom-left grid coordinate</param>
    /// <param name="footprint">Size in grid cells (width x depth)</param>
    /// <param name="isValid">Whether placement is valid</param>
    void ShowFootprint(GroundCoord origin, Vector2Int footprint, bool isValid);
    
    /// <summary>
    /// Show structure footprint preview at exact world position (no grid snapping).
    /// </summary>
    /// <param name="worldPos">World position (footprint origin)</param>
    /// <param name="footprint">Size in grid cells (width x depth)</param>
    /// <param name="isValid">Whether placement is valid</param>
    void ShowFootprintAtWorldPos(Vector3 worldPos, Vector2Int footprint, bool isValid);
    
    /// <summary>
    /// Hide the footprint preview.
    /// </summary>
    void Hide();
  }
}
