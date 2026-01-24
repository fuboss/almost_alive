using Content.Scripts.Building.Services;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Agent capable of building structures.
  /// </summary>
  public interface IBuilderAgent {
    StructuresModule structuresModule { get; }
  }
}
