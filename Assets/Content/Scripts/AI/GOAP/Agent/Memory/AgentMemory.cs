using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent.Memory {
  public struct MemoryConsolidationStats {
    public int ForgottenCount;
    public float TimeSinceReinforcement;
    public int TrackedMemories;
  }

  [Serializable]
  public class AgentMemory {
    public enum RememberResult {
      NewMemory,
      UpdatedMemory,
      Failed
    }

    [ShowInInspector] private List<MemorySnapshot> _memory = new();
    [ShowInInspector] private Dictionary<string, HashSet<MemorySnapshot>> _tagIndex = new();
    [ShowInInspector] private Dictionary<ActorDescription, MemorySnapshot> _targetIndex = new();

    /// <summary>Key-value store for persistent agent data (camp, ownership, etc).</summary>
    public AgentMemoryK persistentMemory { get; } = new();

    private readonly Stack<List<MemorySnapshot>> _listPool = new();
    private Octree<MemorySnapshot> _octree;
    public int count => _memory.Count;

    public void Initialize(Bounds worldBounds) {
      _octree = new Octree<MemorySnapshot>(worldBounds, 1f);
    }

    /// <summary>O(1) check if memory contains snapshot for given target.</summary>
    public bool MemoryContains(MemorySnapshot snapshot) {
      if (snapshot?.target == null) return false;
      return _targetIndex.ContainsKey(snapshot.target);
    }

    /// <summary>O(1) check if memory contains snapshot for given actor.</summary>
    public bool MemoryContains(ActorDescription target) {
      return target != null && _targetIndex.ContainsKey(target);
    }

    /// <summary>O(1) get snapshot by target actor.</summary>
    public bool TryRecallByTarget(ActorDescription target, out MemorySnapshot snapshot) {
      snapshot = null;
      if (target == null) return false;
      return _targetIndex.TryGetValue(target, out snapshot);
    }

    /// <summary>O(1) get snapshot by target actor.</summary>
    public MemorySnapshot RecallTarget(ActorDescription target) {
      if (target == null) return null;
      return _targetIndex.GetValueOrDefault(target);
    }

    public RememberResult TryRemember(MemorySnapshot snapshot) {
      if (snapshot == null) return RememberResult.Failed;

      if (MemoryContains(snapshot)) {
        snapshot.lastUpdateTime = DateTime.UtcNow;
        UpdateIndexForSnapshot(snapshot);
        return RememberResult.UpdatedMemory;
      }

      snapshot.creationTime = DateTime.UtcNow;
      snapshot.lastUpdateTime = snapshot.creationTime;
      _memory.Add(snapshot);
      AddToIndex(snapshot);

      return RememberResult.NewMemory;
    }
    
    public bool Recall(Func<MemorySnapshot, bool> predicate, out MemorySnapshot result) {
      result = default;
      if (predicate == null) return false;
      foreach (var memSnapshot in _memory) {
        if (memSnapshot == null) continue;
        if (memSnapshot.IsExpired) continue;
        if (!predicate(memSnapshot)) continue;
        result = memSnapshot;
        return true;
      }

      return false;
    }

    public MemorySnapshot IsValid(MemorySnapshot snapshot, bool includeOutdated = false) {
      if (snapshot == null) return null;
      if (!MemoryContains(snapshot)) return null;
      if (!includeOutdated && snapshot.IsExpired) return null;
      return snapshot;
    }

    public void Forget(MemorySnapshot snapshot) {
      if (snapshot?.target == null) return;
      if (_targetIndex.Remove(snapshot.target)) {
        _memory.Remove(snapshot);
        RemoveFromIndex(snapshot);
      }
    }

    /// <summary>O(1) forget by actor target.</summary>
    public void Forget(ActorDescription actorToForget) {
      if (actorToForget == null) return;
      if (_targetIndex.TryGetValue(actorToForget, out var snapshot)) {
        Forget(snapshot);
      }
    }

    public void Clear() {
      _memory.Clear();
      _tagIndex.Clear();
      _targetIndex.Clear();
    }

    public void PurgeExpired() {
      var now = DateTime.UtcNow;
      var toRemove = new List<MemorySnapshot>();
      foreach (var snap in _memory) {
        if (snap == null) continue;
        if (snap.target == null || snap.IsExpiredAt(now)) {
          toRemove.Add(snap);
          continue;
        }

        // if (snap.target.GetComponentInParent<ActorInventory>() != null) {
        //   toRemove.Add(snap);
        // }
      }

      foreach (var s in toRemove) Forget(s);
    }

    public void UpdateTags(MemorySnapshot snapshot, IEnumerable<string> newTags) {
      if (snapshot == null) return;
      RemoveFromIndex(snapshot);
      snapshot.tags = newTags?.ToList();
      snapshot.lastUpdateTime = DateTime.UtcNow;
      AddToIndex(snapshot);
    }

    public void AddTags(MemorySnapshot snapshot, IEnumerable<string> moreTags) {
      if (snapshot == null || moreTags == null) return;

      snapshot.tags ??= new List<string>();
      foreach (var t in moreTags) {
        if (snapshot.tags.Contains(t)) continue;

        snapshot.tags.Add(t);
        if (!_tagIndex.TryGetValue(t, out var set)) {
          set = new HashSet<MemorySnapshot>();
          _tagIndex[t] = set;
        }

        set.Add(snapshot);
      }

      snapshot.lastUpdateTime = DateTime.UtcNow;
    }

    public void RemoveTags(MemorySnapshot snapshot, IEnumerable<string> tagsToRemove) {
      if (snapshot == null || tagsToRemove == null || snapshot.tags == null) return;
      foreach (var t in tagsToRemove) {
        if (!snapshot.tags.Remove(t)) continue;
        if (_tagIndex.TryGetValue(t, out var set)) {
          set.Remove(snapshot);
          if (set.Count == 0) _tagIndex.Remove(t);
        }
      }

      snapshot.lastUpdateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if memory contains at least one snapshot with all specified tags. No allocation.
    /// </summary>
    public bool HasWithAllTags(string[] tags, bool includeOutdated = false) {
      if (tags == null || tags.Length == 0) return false;

      HashSet<MemorySnapshot> intersection = null;
      foreach (var tag in tags) {
        if (!_tagIndex.TryGetValue(tag, out var set) || set.Count == 0) {
          return false;
        }

        if (intersection == null) {
          intersection = set;
        } else {
          // Check intersection without allocation
          bool found = false;
          foreach (var snap in intersection) {
            if (set.Contains(snap)) {
              found = true;
              break;
            }
          }
          if (!found) return false;
        }
      }

      if (intersection == null) return false;

      // Check if any is not expired
      foreach (var snap in intersection) {
        if (includeOutdated || !snap.IsExpired) return true;
      }
      return false;
    }

    /// <summary>
    /// Count snapshots with all specified tags. No array allocation.
    /// </summary>
    public int CountWithAllTags(string[] tags, bool includeOutdated = false) {
      if (tags == null || tags.Length == 0) return 0;

      HashSet<MemorySnapshot> intersection = null;
      foreach (var tag in tags) {
        if (!_tagIndex.TryGetValue(tag, out var set)) {
          return 0;
        }

        if (intersection == null) {
          intersection = new HashSet<MemorySnapshot>(set);
        } else {
          intersection.IntersectWith(set);
        }

        if (intersection.Count == 0) return 0;
      }

      if (intersection == null) return 0;

      var count = 0;
      foreach (var snap in intersection) {
        if (includeOutdated || !snap.IsExpired) count++;
      }
      return count;
    }

    public MemorySnapshot[] GetWithAllTags(string[] tags, bool includeOutdated = false) {
      if (tags == null || tags.Length == 0) return Array.Empty<MemorySnapshot>();

      HashSet<MemorySnapshot> intersection = null;
      foreach (var tag in tags) {
        if (!_tagIndex.TryGetValue(tag, out var set)) {
          return Array.Empty<MemorySnapshot>();
        }

        if (intersection == null) intersection = new HashSet<MemorySnapshot>(set);
        else {
          if (set.Count < intersection.Count) intersection.IntersectWith(set);
          else intersection = intersection.Intersect(set).ToHashSet();
        }

        if (intersection.Count == 0) return Array.Empty<MemorySnapshot>();
      }

      var outList = RentList();
      foreach (var snap in intersection) {
        if (includeOutdated || !snap.IsExpired) outList.Add(snap);
      }

      var arr = outList.ToArray();
      ReleaseList(outList);
      return arr;
    }

    /// <summary>
    /// Check if memory contains at least one snapshot with any of specified tags. No allocation.
    /// </summary>
    public bool HasWithAnyTags(string[] tags, bool includeOutdated = false) {
      if (tags == null || tags.Length == 0) return false;

      foreach (var tag in tags) {
        if (!_tagIndex.TryGetValue(tag, out var set)) continue;
        foreach (var snap in set) {
          if (includeOutdated || !snap.IsExpired) return true;
        }
      }
      return false;
    }

    public MemorySnapshot[] GetWithAnyTags(string[] tags, bool includeOutdated = false) {
      if (tags == null || tags.Length == 0) return Array.Empty<MemorySnapshot>();
      var set = new HashSet<MemorySnapshot>();
      foreach (var tag in tags) {
        if (_tagIndex.TryGetValue(tag, out var s)) {
          foreach (var snap in s) set.Add(snap);
        }
      }

      var outList = RentList();
      foreach (var snap in set) {
        if (includeOutdated || !snap.IsExpired) outList.Add(snap);
      }

      var arr = outList.ToArray();
      ReleaseList(outList);
      return arr;
    }

    public void ForEachWithAllTags(string[] tags, Action<MemorySnapshot> action, bool includeOutdated = false) {
      if (tags == null || tags.Length == 0 || action == null) return;

      HashSet<MemorySnapshot> intersection = null;
      foreach (var tag in tags) {
        if (!_tagIndex.TryGetValue(tag, out var set)) return;
        if (intersection == null) intersection = new HashSet<MemorySnapshot>(set);
        else {
          if (set.Count < intersection.Count) intersection.IntersectWith(set);
          else intersection = intersection.Intersect(set).ToHashSet();
        }

        if (intersection.Count == 0) return;
      }

      if (intersection == null) return;
      foreach (var snap in intersection) {
        if (includeOutdated || !snap.IsExpired) action(snap);
      }
    }


    public MemorySnapshot GetNearest(Vector3 position, string[] tags = null,
      Func<MemorySnapshot, bool> predicate = null, bool includeOutdated = false,
      bool sortByConfidence = false) { //todo: impl sortByConfidence
      return Remember(position, tags, predicate, includeOutdated);
    }

    private MemorySnapshot Remember(Vector3 position, string[] tags, Func<MemorySnapshot, bool> predicate,
      bool includeOutdated) {
      if (tags == null || tags.Length == 0) {
        return Remember(position, predicate, includeOutdated);
      }

      MemorySnapshot best = null;
      var bestDist = float.MaxValue;
      ForEachWithAllTags(tags, snap => {
        if (predicate != null && !predicate(snap)) return;
        var d = (snap.location - position).sqrMagnitude;
        if (d < bestDist) {
          bestDist = d;
          best = snap;
        }
      }, includeOutdated);

      return best;
    }

    private MemorySnapshot Remember(Vector3 position, Func<MemorySnapshot, bool> predicate, bool includeOutdated) {
      MemorySnapshot best = null;
      var bestDist = float.MaxValue;
      foreach (var snap in _memory) {
        if (snap == null) continue;
        if (!includeOutdated && snap.IsExpired) continue;
        if (predicate != null && !predicate(snap)) continue;
        var d = (snap.location - position).sqrMagnitude;
        if (d < bestDist) {
          bestDist = d;
          best = snap;
        }
      }

      return best;
    }

    public MemorySnapshot GetNearest(Vector3 position, float maxDistance, string[] tags = null,
      Func<MemorySnapshot, bool> predicate = null, bool includeOutdated = false) {
      var candidates = GetInRadius(position, maxDistance, tags, predicate, includeOutdated);

      MemorySnapshot nearest = null;
      var minDist = float.MaxValue;

      foreach (var snapshot in candidates) {
        var dist = Vector3.Distance(position, snapshot.location);
        if (dist < minDist) {
          minDist = dist;
          nearest = snapshot;
        }
      }

      return nearest;
    }

    public MemorySnapshot[] GetInRadius(Vector3 position, float radius, string[] tags = null,
      Func<MemorySnapshot, bool> predicate = null, bool includeOutdated = false) {
      var bounds = new Bounds(position, Vector3.one * radius * 2f);
      var candidates = _octree.Query(bounds);
      var results = new List<MemorySnapshot>();

      foreach (var snapshot in candidates) {
        if (!includeOutdated && snapshot.IsExpired) continue;
        if (Vector3.Distance(position, snapshot.location) > radius) continue;
        if (tags != null && !HasAllTags(snapshot, tags)) continue;
        if (predicate != null && !predicate(snapshot)) continue;

        results.Add(snapshot);
      }

      return results.ToArray();
    }

    private List<MemorySnapshot> RentList() {
      return _listPool.Count > 0 ? _listPool.Pop() : new List<MemorySnapshot>();
    }

    private void ReleaseList(List<MemorySnapshot> list) {
      list.Clear();
      _listPool.Push(list);
    }

    private void AddToIndex(MemorySnapshot snapshot) {
      if (snapshot == null) return;
      
      // Target index
      if (snapshot.target != null) {
        _targetIndex[snapshot.target] = snapshot;
      }
      
      // Tag index
      if (snapshot.tags != null) {
        foreach (var t in snapshot.tags) {
          if (!_tagIndex.TryGetValue(t, out var set)) {
            set = new HashSet<MemorySnapshot>();
            _tagIndex[t] = set;
          }
          set.Add(snapshot);
        }
      }

      // Spatial index
      _octree?.Remove(snapshot);
      _octree?.Add(snapshot, snapshot.location);
    }

    private void RemoveFromIndex(MemorySnapshot snapshot) {
      if (snapshot == null) return;
      
      // Target index
      if (snapshot.target != null) {
        _targetIndex.Remove(snapshot.target);
      }
      
      // Tag index
      if (snapshot.tags != null) {
        foreach (var tag in snapshot.tags) {
          if (!_tagIndex.TryGetValue(tag, out var set)) continue;
          set.Remove(snapshot);
          if (set.Count == 0) _tagIndex.Remove(tag);
        }
      }

      // Spatial index
      _octree?.Remove(snapshot);
    }

    private void UpdateIndexForSnapshot(MemorySnapshot snapshot) {
      RemoveFromIndex(snapshot);
      AddToIndex(snapshot);
    }

    private bool HasAllTags(MemorySnapshot snapshot, string[] tags) {
      if (snapshot.tags == null || tags == null) return false;
      return tags.All(tag => snapshot.tags.Contains(tag));
    }
  }
}