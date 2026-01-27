#if UNITY_EDITOR
using System;
using System.IO;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.AAWizard {
  [Serializable]
  public class GOAPFeatureWizardComposite {
    [HideInInspector] public bool _init;
    
    private string _featureName = "NewFeature";
    private bool _createGoal = true;
    private bool _createBelief = true;

    [OnInspectorGUI]
    public void DrawGUI() {
      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Create New GOAP Feature", EditorStyles.boldLabel);
      EditorGUILayout.Space(10);

      _featureName = EditorGUILayout.TextField("Feature Name", _featureName);

      EditorGUILayout.Space(5);
      _createGoal = EditorGUILayout.Toggle("Create Sample Goal", _createGoal);
      _createBelief = EditorGUILayout.Toggle("Create Sample Belief", _createBelief);

      EditorGUILayout.Space(15);

      var valid = !string.IsNullOrWhiteSpace(_featureName) && IsValidFolderName(_featureName);
      
      if (!valid && !string.IsNullOrWhiteSpace(_featureName)) {
        EditorGUILayout.HelpBox("Invalid folder name", MessageType.Warning);
      }
      
      GUI.enabled = valid;
      if (GUILayout.Button("Create Feature", GUILayout.Height(40))) {
        CreateFeature();
      }
      GUI.enabled = true;
    }

    private void CreateFeature() {
      var basePath = $"Assets/Content/Resources/GOAP/{_featureName}";

      if (Directory.Exists(basePath)) {
        EditorUtility.DisplayDialog("Error", $"Feature '{_featureName}' already exists!", "OK");
        return;
      }

      var parentPath = "Assets/Content/Resources/GOAP";
      AssetDatabase.CreateFolder(parentPath, _featureName);
      AssetDatabase.CreateFolder(basePath, "Actions");
      AssetDatabase.CreateFolder(basePath, "Beliefs");
      AssetDatabase.CreateFolder(basePath, "Goals");

      var feature = ScriptableObject.CreateInstance<GoapFeatureSO>();
      feature.goals = new();
      feature.beliefs = new();
      feature.compositeBeliefs = new();
      feature.actionDatas = new();
      AssetDatabase.CreateAsset(feature, $"{basePath}/{_featureName}_FeatureSet.asset");

      if (_createGoal) {
        var goal = ScriptableObject.CreateInstance<GoalSO>();
        AssetDatabase.CreateAsset(goal, $"{basePath}/Goals/goal_{_featureName}.asset");
      }

      if (_createBelief) {
        var belief = ScriptableObject.CreateInstance<BeliefSO>();
        AssetDatabase.CreateAsset(belief, $"{basePath}/Beliefs/{_featureName}_Belief.asset");
      }

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      feature.Refresh();
      EditorUtility.SetDirty(feature);
      AssetDatabase.SaveAssets();

      Selection.activeObject = feature;
      EditorGUIUtility.PingObject(feature);

      Debug.Log($"[GOAPFeatureWizard] Created: {_featureName}");
      
      _featureName = "NewFeature";
      _createGoal = true;
      _createBelief = true;
    }

    private static bool IsValidFolderName(string name) {
      var invalidChars = Path.GetInvalidFileNameChars();
      return name.IndexOfAny(invalidChars) < 0;
    }
  }
}
#endif