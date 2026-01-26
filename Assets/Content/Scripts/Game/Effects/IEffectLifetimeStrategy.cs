namespace Content.Scripts.Game.Effects {
  public interface IEffectLifetimeStrategy {
    bool ShouldComplete(EffectHandle handle, float elapsed);
    void OnStart(EffectHandle handle);
    void OnComplete(EffectHandle handle);
  }
}
