namespace Content.Scripts.Building.Services.Visuals {
  /// <summary>
  /// Linear construction progression: decorations appear based on raw progress 0-1.
  /// Default strategy.
  /// </summary>
  public class LinearProgressionStrategy : IConstructionProgressionStrategy {
    public float GetEffectiveProgress(float rawProgress, Runtime.Visuals.StructureDecoration decoration) {
      return rawProgress;
    }
  }
}
