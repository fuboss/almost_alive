using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Harvesting;
using VContainer.Unity;

namespace Content.Scripts.Game.Work {
  /// <summary>
  /// Determines what work agent should be doing based on priorities and world state.
  /// Stub implementation for now.
  /// </summary>
  public class WorkAssignmentModule : ITickable {
    public void Tick() {
      // TODO: Implement work assignment logic
      // 1. For each agent, get their WorkPriority
      // 2. Determine what work is available (loose items for hauling, etc)
      // 3. Assign work goals via AgentBrain
    }

    /// <summary>
    /// Check if there's any hauling work available.
    /// </summary>
    public static bool HasHaulingWork() {
      // Check if there are loose items AND storages that can accept them
      var decayables = ActorRegistry<Decay.DecayableActor>.all;
      foreach (var decayable in decayables) {
        if (decayable == null) continue;
        var desc = decayable.GetComponent<ActorDescription>();
        if (desc == null) continue;
        
        // Check if any storage wants this item
        foreach (var tag in desc.descriptionData.tags) {
          if (Storage.StorageQuery.AnyStorageNeedsTag(tag)) {
            return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Get recommended work for agent based on priorities and available work.
    /// </summary>
    public static WorkType GetRecommendedWork(IGoapAgent agent) {
      var workPriority = agent.gameObject.GetComponent<WorkPriority>();
      if (workPriority == null) return WorkType.NONE;

      foreach (var workType in workPriority.GetWorksByPriority()) {
        if (IsWorkAvailable(workType)) {
          return workType;
        }
      }

      return WorkType.NONE;
    }

    private static bool IsWorkAvailable(WorkType workType) {
      return workType switch {
        WorkType.HAULING => HasHaulingWork(),
        WorkType.FARMING => HasFarmingWork(),
        // TODO: Add other work type checks
        _ => false
      };
    }

    /// <summary>
    /// Check if there's any farming/harvesting work available.
    /// </summary>
    public static bool HasFarmingWork() {
      foreach (var growth in ActorRegistry<GrowthProgress>.all) {
        if (growth != null && growth.hasYield) return true;
      }
      return false;
    }
  }
}
