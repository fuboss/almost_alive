using System.Collections.Generic;

namespace Content.Scripts.Building.Data {
  /// <summary>
  /// Common interface for construction/crafting requirements.
  /// Implemented by both RecipeData (actors) and ConstructionData (structures).
  /// </summary>
  public interface IConstructionRequirements {
    IReadOnlyList<AI.Craft.RecipeRequiredResource> requiredResources { get; }
    float workRequired { get; }
  }
}
