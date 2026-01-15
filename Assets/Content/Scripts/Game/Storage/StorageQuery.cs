using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Content.Scripts.Game.Storage {
  /// <summary>
  /// Static query helpers for finding storages.
  /// </summary>
  public static class StorageQuery {
    /// <summary>
    /// Find nearest storage that has space for item with given tag.
    /// </summary>
    public static StorageActor FindNearestWithSpaceFor(Vector3 position, string tag) {
      return ActorRegistry<StorageActor>.all
        .GetNearest(position, s => s.HasSpaceFor(tag));
    }

    /// <summary>
    /// Find nearest storage that has space for item with any of given tags.
    /// </summary>
    public static StorageActor FindNearestWithSpaceFor(Vector3 position, string[] tags) {
      return ActorRegistry<StorageActor>.all
        .GetNearest(position, s => s.HasSpaceFor(tags));
    }

    /// <summary>
    /// Find storage with highest priority that accepts given tag.
    /// </summary>
    //TODO

    /// <summary>
    /// Find best storage considering both priority and distance.
    /// </summary>
    //todo:

    /// <summary>
    /// Get all storages that accept given tag.
    /// </summary>
    public static IEnumerable<StorageActor> GetAllFor(string tag) {
      return ActorRegistry<StorageActor>.all
        .Where(s => s.AcceptsTag(tag));
    }

    /// <summary>
    /// Get all storages that have space for given tag.
    /// </summary>
    public static IEnumerable<StorageActor> GetAllWithSpaceFor(string tag) {
      return ActorRegistry<StorageActor>.all
        .Where(s => s.HasSpaceFor(tag));
    }

    /// <summary>
    /// Get all storages sorted by priority (for UI display).
    /// </summary>
    public static IEnumerable<StorageActor> GetAllByPriority() {
      return ActorRegistry<StorageActor>.all.SortedByPriority();
    }

    /// <summary>
    /// Check if any storage needs items with given tag.
    /// </summary>
    public static bool AnyStorageNeedsTag(string tag) {
      return ActorRegistry<StorageActor>.all
        .Any(s => s.HasSpaceFor(tag) && s.priority.isEnabled);
    }
    
    public static bool AnyStorageNeeds(ActorDescription actor) {
      return ActorRegistry<StorageActor>.all
        .Any(s => s.HasSpaceFor(actor.descriptionData.tags[0]) && s.priority.isEnabled);
    }

    /// <summary>
    /// Get total free slots for given tag across all storages.
    /// </summary>
    public static int GetTotalFreeSlotsFor(string tag) {
      return ActorRegistry<StorageActor>.all
        .Where(s => s.AcceptsTag(tag))
        .Sum(s => s.freeSlots);
    }
  }
}
