using Content.Scripts.AI.Animals;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Animal that moves as part of a herd.
  /// </summary>
  public interface IHerdMember {
    int herdId { get; set; }
    HerdingBehavior herdingBehavior { get; }
  }
}
