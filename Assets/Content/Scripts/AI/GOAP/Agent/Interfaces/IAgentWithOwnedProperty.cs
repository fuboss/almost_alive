using System.Collections.Generic;
using Content.Scripts.Building.Runtime;

namespace Content.Scripts.AI.GOAP.Agent {
  public interface IAgentWithOwnedProperty {
    IEnumerable<Structure> GetAvailableStructures();
  }
}