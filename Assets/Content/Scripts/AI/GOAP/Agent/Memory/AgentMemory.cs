using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  [Serializable]
  public class AgentMemory {
    public enum RememberResult {
      NewMemory,
      UpdatedMemory,
      Failed
    }

    [ShowInInspector] private List<MemorySnapshot> _memory = new();
    [ShowInInspector] private Dictionary<string, HashSet<MemorySnapshot>> _tagIndex = new();

    private readonly Stack<List<MemorySnapshot>> _listPool = new();
    public int count => _memory.Count;

    public bool MemoryContains(MemorySnapshot snapshot) {
      return snapshot != null && _memory.Any(ms => ms.Equals(snapshot));
    }

    public RememberResult Remember(MemorySnapshot snapshot) {
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


    // public MemorySnapshot Remember(Vector3 location, IEnumerable<string> tags = null,
    //   Object optionalTarget = null, float lifetimeSeconds = 60f, float confidence = 1f) {
    //   var snap = new MemorySnapshot {
    //     location = location,
    //     optionalTarget = optionalTarget,
    //     optionalTargets = null,
    //     lifetimeSeconds = lifetimeSeconds,
    //     confidence = confidence,
    //     tags = tags?.ToList()
    //   };
    //   return Remember(snap);
    // }

    public bool TryFind<T>(Func<MemorySnapshot, bool> predicate, out T result) {
      result = default;
      if (predicate == null) return false;
      foreach (var memSnapshot in _memory) {
        if (memSnapshot == null) continue;
        if (memSnapshot.IsExpired) continue;
        if (predicate(memSnapshot) && memSnapshot is T typed) {
          result = typed;
          return true;
        }
      }

      return false;
    }

    public MemorySnapshot Recall(MemorySnapshot snapshot, bool includeOutdated = false) {
      if (snapshot == null) return null;
      if (!MemoryContains(snapshot)) return null;
      if (!includeOutdated && snapshot.IsExpired) return null;
      return snapshot;
    }

    public void Forget(MemorySnapshot snapshot) {
      if (snapshot == null) return;
      if (_memory.Remove(snapshot)) {
        RemoveFromIndex(snapshot);
      }
    }

    public void Clear() {
      _memory.Clear();
      _tagIndex.Clear();
    }

    public void PurgeExpired() {
      var now = DateTime.UtcNow;
      var toRemove = new List<MemorySnapshot>();
      foreach (var snap in _memory) {
        if (snap != null && snap.IsExpiredAt(now)) toRemove.Add(snap);
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

    private List<MemorySnapshot> RentList() {
      return _listPool.Count > 0 ? _listPool.Pop() : new List<MemorySnapshot>();
    }

    private void ReleaseList(List<MemorySnapshot> list) {
      list.Clear();
      _listPool.Push(list);
    }

    private void AddToIndex(MemorySnapshot snapshot) {
      if (snapshot?.tags == null) return;
      foreach (var t in snapshot.tags) {
        if (!_tagIndex.TryGetValue(t, out var set)) {
          set = new HashSet<MemorySnapshot>();
          _tagIndex[t] = set;
        }

        set.Add(snapshot);
      }
    }

    private void RemoveFromIndex(MemorySnapshot snapshot) {
      if (snapshot?.tags == null) return;
      foreach (var t in snapshot.tags) {
        if (!_tagIndex.TryGetValue(t, out var set)) continue;
        set.Remove(snapshot);
        if (set.Count == 0) _tagIndex.Remove(t);
      }
    }

    private void UpdateIndexForSnapshot(MemorySnapshot snapshot) {
      RemoveFromIndex(snapshot);
      AddToIndex(snapshot);
    }
  }
}