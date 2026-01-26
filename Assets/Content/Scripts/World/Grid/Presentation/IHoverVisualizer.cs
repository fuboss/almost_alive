namespace Content.Scripts.World.Grid.Presentation {
  /// <summary>
  /// Interface for highlighting single grid cell under mouse cursor.
  /// </summary>
  public interface IHoverVisualizer {
    /// <summary>
    /// Show highlight at specified grid coordinate.
    /// </summary>
    /// <param name="coord">Grid coordinate to highlight</param>
    /// <param name="isValid">Whether placement at this location is valid</param>
    void ShowHover(GroundCoord coord, bool isValid);
    
    /// <summary>
    /// Hide the hover highlight.
    /// </summary>
    void Hide();
  }
}
