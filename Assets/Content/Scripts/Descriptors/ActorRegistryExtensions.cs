using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Content.Scripts.Game {
  public static class ActorRegistryExtensions {
    /// <summary>
    /// Find nearest component by position.
    /// </summary>
    public static T GetNearest<T>(this IReadOnlyCollection<T> items, Vector3 position) 
      where T : Component {
      return GetNearest(items, position, null);
    }

    /// <summary>
    /// Find nearest component matching predicate.
    /// </summary>
    public static T GetNearest<T>(this IReadOnlyCollection<T> items, Vector3 position, 
      Func<T, bool> predicate) where T : Component {
      
      T nearest = null;
      var minDistSq = float.MaxValue;

      foreach (var item in items) {
        if (item == null) continue;
        if (predicate != null && !predicate(item)) continue;

        var distSq = (item.transform.position - position).sqrMagnitude;
        if (distSq < minDistSq) {
          minDistSq = distSq;
          nearest = item;
        }
      }

      return nearest;
    }

    /// <summary>
    /// Filter components by tags (requires ActorDescription).
    /// </summary>
    public static IEnumerable<T> WithTags<T>(this IReadOnlyCollection<T> items, params string[] tags) 
      where T : Component {
      
      if (tags == null || tags.Length == 0) {
        foreach (var item in items) yield return item;
        yield break;
      }

      foreach (var item in items) {
        if (item == null) continue;
        var desc = item.GetComponent<ActorDescription>();
        if (desc != null && desc.HasAllTags(tags)) {
          yield return item;
        }
      }
    }

    /// <summary>
    /// Filter by single tag.
    /// </summary>
    public static IEnumerable<T> WithTag<T>(this IReadOnlyCollection<T> items, string tag) 
      where T : Component {
      return WithTags(items, tag);
    }

    /// <summary>
    /// Sort by distance to position.
    /// </summary>
    public static IEnumerable<T> SortedByDistance<T>(this IReadOnlyCollection<T> items, Vector3 position) 
      where T : Component {
      return items
        .Where(i => i != null)
        .OrderBy(i => (i.transform.position - position).sqrMagnitude);
    }

    /// <summary>
    /// Sort by ActorPriority (highest first).
    /// </summary>
    public static IEnumerable<T> SortedByPriority<T>(this IReadOnlyCollection<T> items) 
      where T : Component {
      return items
        .Where(i => i != null)
        .OrderByDescending(i => {
          var p = i.GetComponent<ActorPriority>();
          return p != null ? p.priority : 0;
        });
    }

    /// <summary>
    /// Sort by priority then by distance (for hauling decisions).
    /// </summary>
    public static IEnumerable<T> SortedByPriorityThenDistance<T>(this IReadOnlyCollection<T> items, 
      Vector3 position, float priorityWeight = 0.5f) where T : Component {
      
      return items
        .Where(i => i != null)
        .OrderByDescending(i => {
          var p = i.GetComponent<ActorPriority>();
          var priority = p != null ? p.priority : 0;
          var dist = Vector3.Distance(i.transform.position, position);
          var maxDist = 100f;
          var normalizedDist = 1f - Mathf.Clamp01(dist / maxDist);
          return priority * priorityWeight + normalizedDist * (1f - priorityWeight);
        });
    }

    /// <summary>
    /// Get all within radius.
    /// </summary>
    public static IEnumerable<T> InRadius<T>(this IReadOnlyCollection<T> items, Vector3 position, float radius) 
      where T : Component {
      
      var radiusSq = radius * radius;
      foreach (var item in items) {
        if (item == null) continue;
        if ((item.transform.position - position).sqrMagnitude <= radiusSq) {
          yield return item;
        }
      }
    }
  }
}
