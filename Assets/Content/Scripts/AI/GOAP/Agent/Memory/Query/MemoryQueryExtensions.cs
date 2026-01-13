using System;
using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent.Memory.Query {
  public static class MemoryQueryExtensions {
    public static MemoryQuery Query(this AgentMemory memory) {
      return new MemoryQuery();
    }

    public static MemorySnapshot[] FindWithTags(this AgentMemory memory, params string[] tags) {
      return new MemoryQuery()
        .WithTags(tags)
        .Execute(memory);
    }

    public static MemorySnapshot FindNearest(this AgentMemory memory, Vector3 position,
      string[] tags = null, float maxDistance = float.MaxValue) {
      var query = new MemoryQuery()
        .InRadius(position, maxDistance)
        .OrderBy(s => Vector3.Distance(s.location, position));

      if (tags != null && tags.Length > 0) {
        query.WithTags(tags);
      }

      return query.ExecuteFirst(memory);
    }

    public static MemorySnapshot[] FindInRadius(this AgentMemory memory, Vector3 position,
      float radius, string[] tags = null) {
      var query = new MemoryQuery()
        .InRadius(position, radius);

      if (tags != null && tags.Length > 0) {
        query.WithTags(tags);
      }

      return query.Execute(memory);
    }

    public static MemorySnapshot[] FindMostConfident(this AgentMemory memory,
      string[] tags = null, int count = 10) {
      var query = new MemoryQuery()
        .OrderBy(s => s.confidence, ascending: false)
        .Take(count);

      if (tags != null && tags.Length > 0) {
        query.WithTags(tags);
      }

      return query.Execute(memory);
    }

    // Internal helper for Query DSL
    internal static MemorySnapshot[] GetAll(this AgentMemory memory, bool includeOutdated = false) {
      var result = new List<MemorySnapshot>();
      var now = DateTime.UtcNow;

      foreach (var snapshot in memory.GetAllSnapshots()) {
        if (!includeOutdated && snapshot.IsExpiredAt(now)) continue;
        result.Add(snapshot);
      }

      return result.ToArray();
    }

    internal static IEnumerable<MemorySnapshot> GetAllSnapshots(this AgentMemory memory) {
      // Access private field via reflection or expose it
      var field = typeof(AgentMemory).GetField("_memory",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

      if (field?.GetValue(memory) is List<MemorySnapshot> list) {
        return list;
      }

      return Array.Empty<MemorySnapshot>();
    }
  }
}