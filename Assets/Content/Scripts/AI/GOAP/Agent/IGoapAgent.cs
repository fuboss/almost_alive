using VContainer.Unity;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Full agent interface combining all capabilities.
  /// Used by GOAPAgent (human colonist).
  /// </summary>
  public interface IGoapAgent : IGoapAgentCore,
    ITransientTargetAgent, 
    IInventoryAgent,
    IWorkAgent, 
    IAgentWithOwnedProperty,
    ITickable {
  }
}
