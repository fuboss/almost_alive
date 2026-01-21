using Content.Scripts.Game;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Agent that can hold items in inventory.
  /// </summary>
  public interface IInventoryAgent {
    ActorInventory inventory { get; }
  }
}
