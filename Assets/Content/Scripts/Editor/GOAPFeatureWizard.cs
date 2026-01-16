#if UNITY_EDITOR
using System.IO;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor {
  public class GOAPFeatureWizard : EditorWindow {
    private string _featureName = "NewFeature";
    private bool _createGoal = true;
    private bool _createBelief = true;

    [MenuItem("GOAP/Create Feature", priority = 1)]
    public static void Open() {
      var window = GetWindow<GOAPFeatureWizard>("Feature Wizard");
      window.minSize = new Vector2(300, 150);
    }

    private void OnGUI() {
      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Create New GOAP Feature", EditorStyles.boldLabel);
      EditorGUILayout.Space(10);

      _featureName = EditorGUILayout.TextField("Feature Name", _featureName);

      EditorGUILayout.Space(5);
      _createGoal = EditorGUILayout.Toggle("Create Sample Goal", _createGoal);
      _createBelief = EditorGUILayout.Toggle("Create Sample Belief", _createBelief);

      EditorGUILayout.Space(15);

      var valid = !string.IsNullOrWhiteSpace(_featureName) && IsValidFolderName(_featureName);
      GUI.enabled = valid;

      if (GUILayout.Button("Create Feature", GUILayout.Height(30))) {
        CreateFeature();
      }

      GUI.enabled = true;

      if (!valid && !string.IsNullOrWhiteSpace(_featureName)) {
        EditorGUILayout.HelpBox("Invalid folder name", MessageType.Warning);
      }
    }

    private void CreateFeature() {
      var basePath = $"Assets/Content/Resources/GOAP/{_featureName}";

      if (Directory.Exists(basePath)) {
        EditorUtility.DisplayDialog("Error", $"Feature '{_featureName}' already exists!", "OK");
        return;
      }

      // Create folders
      var parentPath = "Assets/Content/Resources/GOAP";
      AssetDatabase.CreateFolder(parentPath, _featureName);
      AssetDatabase.CreateFolder(basePath, "Actions");
      AssetDatabase.CreateFolder(basePath, "Beliefs");
      AssetDatabase.CreateFolder(basePath, "Goals");

      // Create FeatureSet
      var feature = CreateInstance<GoatFeatureSO>();
      feature.goals = new();
      feature.beliefs = new();
      feature.compositeBeliefs = new();
      feature.actionDatas = new();
      AssetDatabase.CreateAsset(feature, $"{basePath}/{_featureName}_FeatureSet.asset");

      // Create sample goal if requested
      if (_createGoal) {
        var goal = CreateInstance<GoalSO>();
        AssetDatabase.CreateAsset(goal, $"{basePath}/Goals/goal_{_featureName}.asset");
      }

      // Create sample belief if requested
      if (_createBelief) {
        var belief = CreateInstance<BeliefSO>();
        AssetDatabase.CreateAsset(belief, $"{basePath}/Beliefs/{_featureName}_Belief.asset");
      }

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      // Refresh the feature to pick up created assets
      feature.Refresh();
      EditorUtility.SetDirty(feature);
      AssetDatabase.SaveAssets();

      // Select the created feature
      Selection.activeObject = feature;
      EditorGUIUtility.PingObject(feature);

      Debug.Log($"Created feature: {_featureName}");
      Close();
    }

    private static bool IsValidFolderName(string name) {
      var invalidChars = Path.GetInvalidFileNameChars();
      return name.IndexOfAny(invalidChars) < 0;
    }
  }
}
#endif
