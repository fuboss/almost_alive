using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Content.Scripts.AI.GOAP {
  [CreateAssetMenu(fileName = "FeatureSet", menuName = "GOAP/Feature Set", order = 0)]
  public class GoapFeatureSO : SerializedScriptableObject {
    public List<GoalSO> goals;
    public List<BeliefSO> beliefs;
    public List<CompositeBeliefSO> compositeBeliefs;
    public List<ActionDataSO> actionDatas;

    [Button]
    public void Refresh() {
#if UNITY_EDITOR
      var myPath = AssetDatabase.GetAssetPath(this);
      if (string.IsNullOrEmpty(myPath)) return;

      myPath = myPath.Substring(0, myPath.LastIndexOf('/'));
      actionDatas = Load<ActionDataSO>("t:ActionDataSO", myPath);
      goals = Load<GoalSO>("t:GoalSO", myPath);
      beliefs = Load<BeliefSO>("t:BeliefSO", myPath);
      compositeBeliefs = Load<CompositeBeliefSO>("t:CompositeBeliefSO", myPath);

      EditorUtility.SetDirty(this);
#endif
    }

#if UNITY_EDITOR
    private static List<T> Load<T>(string filter, string path) where T : Object {
      return AssetDatabase.FindAssets(filter, new[] { path })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadAssetAtPath<T>)
        .Where(a => a != null)
        .ToList();
    }
#endif
  }

#if UNITY_EDITOR
  /// <summary>
  /// Auto-refresh feature sets when assets change in their folders.
  /// </summary>
  public class FeatureSetPostProcessor : AssetPostprocessor {
    private static void OnPostprocessAllAssets(
      string[] importedAssets,
      string[] deletedAssets,
      string[] movedAssets,
      string[] movedFromAssetPaths) {
      var changedPaths = importedAssets
        .Concat(deletedAssets)
        .Concat(movedAssets)
        .Concat(movedFromAssetPaths)
        .Where(p => p.StartsWith("Assets/Content/Resources/GOAP"))
        .ToArray();

      if (changedPaths.Length == 0) return;

      // Delay to avoid issues during import
      EditorApplication.delayCall += () => RefreshAffectedFeatures(changedPaths);
    }

    private static void RefreshAffectedFeatures(string[] changedPaths) {
      var features = AssetDatabase.FindAssets("t:GoatFeatureSO", new[] { "Assets/Content/Resources/GOAP" })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadAssetAtPath<GoapFeatureSO>)
        .Where(f => f != null)
        .ToList();

      var refreshed = false;
      foreach (var feature in features) {
        var featurePath = AssetDatabase.GetAssetPath(feature);
        var featureFolder = featurePath.Substring(0, featurePath.LastIndexOf('/'));

        if (changedPaths.Any(p => p.StartsWith(featureFolder) && !p.EndsWith("_FeatureSet.asset"))) {
          feature.Refresh();
          refreshed = true;
        }
      }

      if (refreshed) {
        GOAPEditorHelper.InvalidateCache();
        AssetDatabase.SaveAssets();
      }
    }
  }
#endif
}
