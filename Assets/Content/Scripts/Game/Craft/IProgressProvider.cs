namespace Content.Scripts.Game.Craft {
  public interface IProgressProvider {
    float progress { get; }
    ActorDescription actor { get; }
  }
}