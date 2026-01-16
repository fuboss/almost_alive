using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Content.Scripts.AI.GOAP {
  public static class GOAPEditorHelper {
    private static List<string> _cachedBeliefNames;
    private static bool _beliefsCacheValid;

    public static void InvalidateCache() {
      _beliefsCacheValid = false;
      _cachedBeliefNames = null;
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
      return TagRegistry.AllTags.ToList();
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
