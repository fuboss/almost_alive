using Content.Scripts.Game;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Agent that can focus on a transient interaction target.
  /// Used for pickup, deposit, attack, and other targeted interactions.
  /// </summary>
  public interface ITransientTargetAgent {
    ActorDescription transientTarget { get; set; }
    int transientTargetId { get; }
  }
}
