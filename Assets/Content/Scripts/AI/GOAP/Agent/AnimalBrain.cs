namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Simplified brain for animal agents.
  /// No interruptions, no plan stack - just core GOAP planning.
  /// </summary>
  public class AnimalBrain : AgentBrainBase {
    protected override void OnPlanCleared() {
      // Animals have no transient target to clear
    }
  }
}
