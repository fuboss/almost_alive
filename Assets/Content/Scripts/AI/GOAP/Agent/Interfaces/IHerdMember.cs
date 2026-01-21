namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Animal agent that moves as part of a herd.
  /// Will be implemented in Phase 3 (Herding System).
  /// </summary>
  public interface IHerdMember {
    // HerdingBehavior herdBehavior { get; }
    int herdId { get; set; }
  }
}
