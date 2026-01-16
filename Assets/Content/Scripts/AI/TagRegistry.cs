using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Game;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Content.Scripts.AI {
  /// <summary>
  /// Dynamic tag registry. Collects tags from all TagDefinition types.
  /// </summary>
  public static class TagRegistry {
    private static string[] _cachedTags;
    private static bool _initialized;

    public static string[] AllTags {
      get {
        if (!_initialized) RefreshTags();
        return _cachedTags;
      }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void RefreshTags() {
#if UNITY_EDITOR
      var types = TypeCache.GetTypesDerivedFrom<TagDefinition>()
        .Where(t => !t.IsAbstract && !t.IsGenericType);

      var tags = new List<string>();
      foreach (var type in types) {
        try {
          var instance = (TagDefinition)Activator.CreateInstance(type);
          if (!string.IsNullOrEmpty(instance.Tag)) {
            tags.Add(instance.Tag);
          }
        }
        catch {
          // Skip types that can't be instantiated
        }
      }

      _cachedTags = tags.Distinct().OrderBy(t => t).ToArray();
#else
      _cachedTags = Tag.ALL_TAGS;
#endif
      _initialized = true;
    }

    public static void Invalidate() {
      _initialized = false;
      _cachedTags = null;
    }

#if UNITY_EDITOR
    public static List<string> GetTagsForDropdown() {
      return AllTags.ToList();
    }
#endif
  }
}
