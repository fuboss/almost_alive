// csharp

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent.Memory {
  public class MemorySnapshot : IEquatable<MemorySnapshot>, IEqualityComparer<MemorySnapshot> {
    public List<string> tags;
    public Vector3 location;

    public GameObject target;
    public List<GameObject> optionalTargets;

    public bool isOutdated;
    public DateTime creationTime;
    public DateTime lastUpdateTime;
    public float confidence; // 0-1
    public float lifetimeSeconds;

    public bool IsExpired => IsExpiredAt(DateTime.UtcNow);

    public bool IsExpiredAt(DateTime time) {
      if (isOutdated) return true;
      if (lifetimeSeconds <= 0f) return false;
      var age = (time - creationTime).TotalSeconds;
      return age > lifetimeSeconds;
    }

    public void AddTag(string tag) {
      if (string.IsNullOrEmpty(tag)) return;
      tags ??= new List<string>();
      if (!tags.Contains(tag)) tags.Add(tag);
      lastUpdateTime = DateTime.UtcNow;
    }

    public void RemoveTag(string tag) {
      if (tags == null) return;
      if (tags.Remove(tag)) lastUpdateTime = DateTime.UtcNow;
    }

    public bool HasTag(string tag) {
      return tags != null && tags.Contains(tag);
    }

    public void Reset() {
      tags?.Clear();
      location = default;
      target = null;
      optionalTargets?.Clear();
      isOutdated = false;
      creationTime = DateTime.MinValue;
      lastUpdateTime = DateTime.MinValue;
      confidence = 0f;
      lifetimeSeconds = 0f;
    }

    bool IEquatable<MemorySnapshot>.Equals(MemorySnapshot other) {
      return other != null && Equals(this, other);
    }

    public bool Equals(MemorySnapshot x, MemorySnapshot y) {
      if (ReferenceEquals(x, y)) return true;
      if (x is null) return false;
      if (y is null) return false;
      return x.GetType() == y.GetType() && Equals(x.target, y.target);
    }

    public int GetHashCode(MemorySnapshot obj) {
      return HashCode.Combine(obj.tags, obj.target);
    }

    public override bool Equals(object obj) {
      if (obj is MemorySnapshot other) {
        return Equals(this, other);
      }

      return base.Equals(obj);
    }

    public override int GetHashCode() => GetHashCode(this);
  }
}