using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Content.Scripts.Game.Storage {
  /// <summary>
  /// Static query helpers for finding storages.
  /// Uses frame-based caching for frequently called methods.
  /// </summary>
  public static class StorageQuery {
    // Frame-based cache for AnyStorageNeeds
    private static int _cachedFrame = -1;
    private static readonly HashSet<string> _acceptedTagsCache = new();
    private static readonly Dictionary<int, bool> _actorNeedsCache = new();

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void InitEditor() {
      UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state) {
      if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
        ClearCache();
      }
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() {
      ClearCache();
    }

    /// <summary>
    /// Clears all cached data. Called automatically on play mode exit.
    /// </summary>
    public static void ClearCache() {
      _cachedFrame = -1;
      _acceptedTagsCache.Clear();
      _actorNeedsCache.Clear();
    }

    /// <summary>
    /// Rebuilds cache if frame changed. Call at start of frame or lazily on first query.
    /// </summary>
    private static void EnsureCacheValid() {
      var currentFrame = Time.frameCount;
      if (_cachedFrame == currentFrame) return;
      
      _cachedFrame = currentFrame;
      _acceptedTagsCache.Clear();
      _actorNeedsCache.Clear();
      
      // Build set of all tags that enabled storages accept and have space for
      foreach (var storage in ActorRegistry<StorageActor>.all) {
        if (!storage.priority.isEnabled) continue;
        if (storage.isFull) continue;
        
        var tags = storage.acceptedTags;
        if (tags == null) continue;
        
        foreach (var t in tags) {
          _acceptedTagsCache.Add(t);
        }
      }
    }

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
    /// Check if any enabled storage needs items with given tag. Uses frame cache.
    /// </summary>
    public static bool AnyStorageNeedsTag(string tag) {
      EnsureCacheValid();
      return _acceptedTagsCache.Contains(tag);
    }

    /// <summary>
    /// Check if any enabled storage can accept this actor. Uses frame cache.
    /// </summary>
    public static bool AnyStorageNeeds(ActorDescription actor) {
      if (actor == null) return false;
      
      EnsureCacheValid();
      
      // Check actor cache first
      var actorId = actor.actorId;
      if (_actorNeedsCache.TryGetValue(actorId, out var cached)) {
        return cached;
      }
      
      // Check if actor has any tag that storages need
      var result = false;
      var tags = actor.descriptionData?.tags;
      if (tags != null) {
        foreach (var tag in tags) {
          if (_acceptedTagsCache.Contains(tag)) {
            result = true;
            break;
          }
        }
      }
      
      _actorNeedsCache[actorId] = result;
      return result;
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