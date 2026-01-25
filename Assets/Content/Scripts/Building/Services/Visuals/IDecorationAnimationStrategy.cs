namespace Content.Scripts.Building.Services.Visuals {
  /// <summary>
  /// Strategy for animating decoration show/hide.
  /// </summary>
  public interface IDecorationAnimationStrategy {
    /// <summary>
    /// Animate decoration to visible state.
    /// </summary>
    void Show(Runtime.Visuals.StructureDecoration decoration, float duration);
    
    /// <summary>
    /// Animate decoration to hidden state.
    /// </summary>
    void Hide(Runtime.Visuals.StructureDecoration decoration, float duration);
  }
}
