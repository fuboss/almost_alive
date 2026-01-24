using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.Camp;
using UnityEngine;

namespace Content.Scripts.Game.Craft {
  /// <summary>
  /// Static query helpers for finding unfinished actors.
  /// </summary>
  public static class UnfinishedQuery {
    /// <summary>Find unfinished that needs specific resource.</summary>
    public static UnfinishedActor GetNeedingResource(string tag) {
      return ActorRegistry<UnfinishedActor>.all
        .FirstOrDefault(u => u.GetRemainingResourceCount(tag) > 0);
    }

    /// <summary>Find unfinished that needs any resources.</summary>
    public static UnfinishedActor GetNeedingResources() {
      return ActorRegistry<UnfinishedActor>.all
        .FirstOrDefault(u => !u.hasAllResources);
    }

    public static IEnumerable<UnfinishedActor> GetAllNeedingResources() {
      return ActorRegistry<UnfinishedActor>.all
        .Where(u => !u.hasAllResources);
    }

    /// <summary>Find unfinished that needs work.</summary>
    public static UnfinishedActor GetNeedingWork() {
      return ActorRegistry<UnfinishedActor>.all
        .FirstOrDefault(u => u.hasAllResources && !u.workComplete);
    }

    /// <summary>Find unfinished that is ready to complete.</summary>
    public static UnfinishedActor GetReadyToComplete() {
      return ActorRegistry<UnfinishedActor>.all
        .FirstOrDefault(u => u.isReadyToComplete);
    }


    public static bool HasActiveUnfinished() {
      return ActorRegistry<UnfinishedActor>.count > 0;
    }
  }
}