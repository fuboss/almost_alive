using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Content.Scripts.AI.GOAP {
  public static class GOAPEditorHelper {
    public static List<string> GetBeliefsNames() {
      var l = new List<string>();
#if UNITY_EDITOR
      AssetDatabase.FindAssets("t:BeliefSO", new[] { "Assets/Content/Resources/GOAP" })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadAssetAtPath<BeliefSO>)
        .ToList()
        .ForEach(so => l.Add(so.name));
#endif
      return l;
    }

    public static List<string> GetGoalsNames() {
      var l = new List<string>();
#if UNITY_EDITOR
      AssetDatabase.FindAssets("t:GoalSO", new[] { "Assets/Content/Resources/GOAP" })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadAssetAtPath<GoalSO>)
        .ToList()
        .ForEach(so => l.Add(so.name));
#endif
      return l;
    }
  }
}

// using Content.Scripts.AI.GOAP.Agent;
// using UnityEditor;
// using UnityEngine;
//
// namespace Content.Scripts.AI.GOAP.Editor {
//   [CustomEditor(typeof(GOAPAgent))]
//   public class GOAPAgentInspector : UnityEditor.Editor {
//     public override void OnInspectorGUI() {
//       var agent = (GOAPAgent)target;
//
//       EditorGUILayout.Space();
//       DrawDefaultInspector();
//
//       EditorGUILayout.Space();
//
//       if (agent.currentGoal != null) {
//         EditorGUILayout.LabelField("Current Goal:", EditorStyles.boldLabel);
//         EditorGUILayout.BeginHorizontal();
//         GUILayout.Space(10);
//         EditorGUILayout.LabelField(agent.currentGoal.Name);
//         EditorGUILayout.EndHorizontal();
//       }
//
//       EditorGUILayout.Space();
//
//       // Show current action
//       if (agent.currentAction != null) {
//         EditorGUILayout.LabelField("Current Action:", EditorStyles.boldLabel);
//         EditorGUILayout.BeginHorizontal();
//         GUILayout.Space(10);
//         EditorGUILayout.LabelField(agent.currentAction.Name);
//         EditorGUILayout.EndHorizontal();
//       }
//
//       EditorGUILayout.Space();
//
//       // Show current plan
//       if (agent.actionPlan != null) {
//         EditorGUILayout.LabelField("Plan Stack:", EditorStyles.boldLabel);
//         foreach (var a in agent.actionPlan.Actions) {
//           EditorGUILayout.BeginHorizontal();
//           GUILayout.Space(10);
//           EditorGUILayout.LabelField(a.Name);
//           EditorGUILayout.EndHorizontal();
//         }
//       }
//
//       EditorGUILayout.Space();
//
//       // Show beliefs
//       EditorGUILayout.LabelField("Beliefs:", EditorStyles.boldLabel);
//       if (agent.beliefs != null)
//         foreach (var belief in agent.beliefs) {
//           if (belief.Key is "Nothing" or "Something") continue;
//           EditorGUILayout.BeginHorizontal();
//           GUILayout.Space(10);
//           EditorGUILayout.LabelField(belief.Key + ": " + belief.Value.Evaluate());
//           EditorGUILayout.EndHorizontal();
//         }
//
//       EditorGUILayout.Space();
//     }
//   }
// }