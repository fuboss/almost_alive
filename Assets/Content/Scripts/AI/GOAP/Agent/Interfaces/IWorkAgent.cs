using Content.Scripts.AI.Craft;
using Content.Scripts.Game.Work;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Agent that can work, gain experience, and craft.
  /// </summary>
  public interface IWorkAgent {
    AgentExperience experience { get; }
    AgentRecipes recipes { get; }
    RecipeModule recipeModule { get; }
    WorkPriority GetWorkScheduler();
    void AddExperience(int amount);
  }
}
