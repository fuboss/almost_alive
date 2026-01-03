using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP {
  [CreateAssetMenu(fileName = "FeatureSet", menuName = "GOAP/Feature Set", order = 0)]
  public class GoatFeatureSO : SerializedScriptableObject {
    public List<GoalSO> goals;
    public List<BeliefSO> beliefs;
    public List<ActionDataSO> actionDatas;


    [Button]
    private void Refresh() {
      Debug.LogError("TDODO!!!");
#if !UNITY_EDITOR
      return;
#endif
      
      var myPath = UnityEditor.AssetDatabase.GetAssetPath(this);
      myPath = myPath.Substring(0, myPath.LastIndexOf('/'));
      actionDatas = Load<ActionDataSO>("t:ActionDataSO", myPath);
      goals = Load<GoalSO>("t:GoalSO", myPath);
      beliefs = Load<BeliefSO>("t:BeliefSO", myPath);

      return;

      List<T> Load<T>(string filter, string path) where T : UnityEngine.Object {
        return UnityEditor.AssetDatabase.FindAssets(filter, new[] { path })
          .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
          .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<T>)
          .ToList();
      }
    }
  }
}