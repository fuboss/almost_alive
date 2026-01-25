namespace Content.Scripts.Building.Services.Visuals {
  /// <summary>
  /// Strategy for mapping raw construction progress to effective progress for decorations.
  /// Allows different progression modes: linear, staged, etc.
  /// </summary>
  public interface IConstructionProgressionStrategy {
    /// <summary>
    /// Calculate effective progress for decoration threshold check.
    /// </summary>
    /// <param name="rawProgress">Raw construction progress 0-1</param>
    /// <param name="decoration">Decoration being evaluated</param>
    /// <returns>Effective progress 0-1</returns>
    float GetEffectiveProgress(float rawProgress, Runtime.Visuals.StructureDecoration decoration);
  }
}
