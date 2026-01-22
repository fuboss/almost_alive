namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Interface for animal agents (deer, wolves, rabbits, etc.)
  /// Simplified compared to full IGoapAgent - no inventory, work, camp.
  /// </summary>
  public interface IGoapAnimalAgent : IGoapAgentCore, IHerdMember {
  }
}
