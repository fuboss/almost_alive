using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using Content.Scripts.Game;
#endif

namespace Content.Scripts.AI.GOAP {
  public static class GOAPEditorHelper {
    private static List<string> _cachedBeliefNames;
    private static List<string> _cachedActorKeys;
    private static bool _beliefsCacheValid;
    private static bool _actorsCacheValid;

    public static void InvalidateCache() {
      _beliefsCacheValid = false;
      _actorsCacheValid = false;
      _cachedBeliefNames = null;
      _cachedActorKeys = null;
    }

    public static List<string> GetBeliefsNames() {
#if UNITY_EDITOR
      if (_beliefsCacheValid && _cachedBeliefNames != null) {
        return _cachedBeliefNames;
      }

      var names = new List<string>();

      AssetDatabase.FindAssets("t:BeliefSO", new[] { "Assets/Content/Resources/GOAP" })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadAssetAtPath<BeliefSO>)
        .Where(so => so != null)
        .ToList()
        .ForEach(so => names.Add(so.name));

      AssetDatabase.FindAssets("t:CompositeBeliefSO", new[] { "Assets/Content/Resources/GOAP" })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadAssetAtPath<CompositeBeliefSO>)
        .Where(comp => comp != null)
        .SelectMany(comp => comp.Get())
        .Where(b => b != null)
        .ToList()
        .ForEach(b => names.Add(b.name));

      _cachedBeliefNames = names.Distinct().OrderBy(n => n).ToList();
      _beliefsCacheValid = true;
      return _cachedBeliefNames;
#else
      return new List<string>();
#endif
    }

    public static HashSet<string> GetBeliefsNamesSet() {
      return new HashSet<string>(GetBeliefsNames());
    }

    public static List<string> GetGoalsNames() {
      var l = new List<string>();
#if UNITY_EDITOR
      AssetDatabase.FindAssets("t:GoalSO", new[] { "Assets/Content/Resources/GOAP" })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadAssetAtPath<GoalSO>)
        .Where(so => so != null)
        .ToList()
        .ForEach(so => l.Add(so.name));
#endif
      return l;
    }

    public static List<string> GetTags() {
      return Tag.ALL_TAGS.ToList();
    }

    public static GameObject GetActorPrefab(string actorKey) {
#if !UNITY_EDITOR
      return nul;
#endif
      var results = new List<AddressableAssetEntry>();
      AddressableAssetSettingsDefaultObject.Settings.FindGroup("Actors").GatherAllAssets(results, true, true, false,
        entry => entry.address == actorKey);
      return results.FirstOrDefault()?.MainAsset as GameObject;
    }

    /// <summary>
    /// Get all actor keys from Addressables with "Actors" label.
    /// </summary>
    public static List<string> GetActorKeys() {
#if UNITY_EDITOR
      if (_actorsCacheValid && _cachedActorKeys != null) {
        return _cachedActorKeys;
      }

      _cachedActorKeys = new List<string>();
      var settings = AddressableAssetSettingsDefaultObject.Settings;
      if (settings == null) return _cachedActorKeys;

      foreach (var group in settings.groups) {
        if (group == null) continue;
        foreach (var entry in group.entries) {
          if (!entry.labels.Contains("Actors")) continue;

          var go = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(entry.AssetPath);
          if (go == null) continue;

          var actor = go.GetComponent<ActorDescription>();
          if (actor != null && !string.IsNullOrEmpty(actor.actorKey)) {
            _cachedActorKeys.Add(actor.actorKey);
          }
        }
      }

      _cachedActorKeys = _cachedActorKeys.Distinct().OrderBy(k => k).ToList();
      _actorsCacheValid = true;
      return _cachedActorKeys;
#else
      return new List<string>();
#endif
    }

    /// <summary>
    /// Validate belief references exist.
    /// </summary>
    public static List<string> ValidateBeliefReferences(IEnumerable<string> references) {
      var allBeliefs = GetBeliefsNamesSet();
      return references
        .Where(r => !string.IsNullOrEmpty(r) && !allBeliefs.Contains(r))
        .ToList();
    }
  }
}