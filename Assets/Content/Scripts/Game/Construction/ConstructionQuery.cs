using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.Camp;
using UnityEngine;

namespace Content.Scripts.Game.Construction {
  /// <summary>
  /// Static query helpers for finding construction sites.
  /// </summary>
  public static class ConstructionQuery {
    /// <summary>Find construction site at specific camp.</summary>
    public static ConstructionSiteActor GetAtCamp(CampLocation camp) {
      if (camp?.setup == null) return null;
      return ActorRegistry<ConstructionSiteActor>.all
        .FirstOrDefault(c => c.assignedSpot != null && 
                             camp.setup.spots.Contains(c.assignedSpot));
    }

    /// <summary>Find all construction sites at camp.</summary>
    public static IEnumerable<ConstructionSiteActor> GetAllAtCamp(CampLocation camp) {
      if (camp?.setup == null) return Enumerable.Empty<ConstructionSiteActor>();
      return ActorRegistry<ConstructionSiteActor>.all
        .Where(c => c.assignedSpot != null && camp.setup.spots.Contains(c.assignedSpot));
    }

    /// <summary>Find construction site that needs specific resource.</summary>
    public static ConstructionSiteActor GetNeedingResource(CampLocation camp, string tag) {
      return GetAllAtCamp(camp).FirstOrDefault(c => c.GetRemainingResourceCount(tag) > 0);
    }

    /// <summary>Find construction site that needs any resources.</summary>
    public static ConstructionSiteActor GetNeedingResources(CampLocation camp) {
      return GetAllAtCamp(camp).FirstOrDefault(c => !c.hasAllResources);
    }

    /// <summary>Find construction site that needs work.</summary>
    public static ConstructionSiteActor GetNeedingWork(CampLocation camp) {
      return GetAllAtCamp(camp).FirstOrDefault(c => c.hasAllResources && !c.workComplete);
    }

    /// <summary>Find construction site that is ready to complete.</summary>
    public static ConstructionSiteActor GetReadyToComplete(CampLocation camp) {
      return GetAllAtCamp(camp).FirstOrDefault(c => c.isReadyToComplete);
    }

    /// <summary>Find nearest construction site that needs resources.</summary>
    public static ConstructionSiteActor GetNearestNeedingResources(Vector3 position) {
      return ActorRegistry<ConstructionSiteActor>.all
        .GetNearest(position, c => !c.hasAllResources);
    }

    /// <summary>Check if camp has any active construction.</summary>
    public static bool HasActiveConstruction(CampLocation camp) {
      return GetAtCamp(camp) != null;
    }
  }
}
