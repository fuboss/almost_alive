using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Goals;

namespace Content.Scripts.AI.GOAP.Interruption {
  /// <summary>
  /// Source that can trigger plan interruption for higher-priority goals.
  /// </summary>
  public interface IInterruptionSource {
    /// <summary>
    /// Priority of this interruption source. Higher = checked first.
    /// </summary>
    float priority { get; }
    
    /// <summary>
    /// Check if agent should interrupt current plan.
    /// </summary>
    /// <param name="agent">The agent to check</param>
    /// <param name="currentGoal">Agent's current goal (null if none)</param>
    /// <param name="interruptGoal">Output: the goal to switch to</param>
    /// <returns>True if should interrupt</returns>
    bool ShouldInterrupt(IGoapAgent agent, AgentGoal currentGoal, out AgentGoal interruptGoal);
    
    /// <summary>
    /// Whether to save current plan for later resume.
    /// </summary>
    bool shouldSaveCurrentPlan { get; }
  }
}
