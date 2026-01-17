using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.Camp;
using UnityEngine;

namespace Content.Scripts.Game.Craft {
  /// <summary>
  /// Static query helpers for finding unfinished actors.
  /// </summary>
  public static class UnfinishedQuery {
    /// <summary>Find unfinished at specific camp.</summary>
    public static UnfinishedActor GetAtCamp(CampLocation camp) {
      if (camp?.setup == null) return null;
      return ActorRegistry<UnfinishedActor>.all
        .FirstOrDefault(u => u.assignedSpot != null && 
                             camp.setup.spots.Contains(u.assignedSpot));
    }

    /// <summary>Find all unfinished at camp.</summary>
    public static IEnumerable<UnfinishedActor> GetAllAtCamp(CampLocation camp) {
      if (camp?.setup == null) return Enumerable.Empty<UnfinishedActor>();
      return ActorRegistry<UnfinishedActor>.all
        .Where(u => u.assignedSpot != null && camp.setup.spots.Contains(u.assignedSpot));
    }

    /// <summary>Find unfinished that needs specific resource.</summary>
    public static UnfinishedActor GetNeedingResource(CampLocation camp, string tag) {
      return GetAllAtCamp(camp).FirstOrDefault(u => u.GetRemainingResourceCount(tag) > 0);
    }

    /// <summary>Find unfinished that needs any resources.</summary>
    public static UnfinishedActor GetNeedingResources(CampLocation camp) {
      return GetAllAtCamp(camp).FirstOrDefault(u => !u.hasAllResources);
    }

    /// <summary>Find unfinished that needs work.</summary>
    public static UnfinishedActor GetNeedingWork(CampLocation camp) {
      return GetAllAtCamp(camp).FirstOrDefault(u => u.hasAllResources && !u.workComplete);
    }

    /// <summary>Find unfinished that is ready to complete.</summary>
    public static UnfinishedActor GetReadyToComplete(CampLocation camp) {
      return GetAllAtCamp(camp).FirstOrDefault(u => u.isReadyToComplete);
    }

    /// <summary>Find nearest unfinished that needs resources.</summary>
    public static UnfinishedActor GetNearestNeedingResources(Vector3 position) {
      return ActorRegistry<UnfinishedActor>.all
        .GetNearest(position, u => !u.hasAllResources);
    }

    /// <summary>Check if camp has any active unfinished.</summary>
    public static bool HasActiveUnfinished(CampLocation camp) {
      return GetAtCamp(camp) != null;
    }
  }
}
