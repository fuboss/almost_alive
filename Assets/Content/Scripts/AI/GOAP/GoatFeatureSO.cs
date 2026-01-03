using System.Collections.Generic;
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
    }
  }
}