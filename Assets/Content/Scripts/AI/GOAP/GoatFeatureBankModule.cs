using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using UnityEngine;

namespace Content.Scripts.AI.GOAP {
  public class GoatFeatureBankModule {
    private List<GoatFeatureSO> _featuresSOs;

    private GoatFeatureBankModule(List<GoatFeatureSO> featuresSOs) {
      _featuresSOs = featuresSOs;
      Debug.LogError($"GoatFeatureBankModule:{_featuresSOs?.Count.ToString() ?? "NULL"}");
    }

    public static GoatFeatureBankModule GetFromResources() {
      var goalSOs = Resources.LoadAll<GoatFeatureSO>("GOAP").ToList();
      return new GoatFeatureBankModule(goalSOs);
    }
    
    // public List<FeatureData> GetFeatures(IGoapAgent agent, string[] availableFeatures) {
    //   var features = new List<FeatureData>();
    //   foreach (var featureName in availableFeatures) {
    //     var featureSet = _featuresSOs.FirstOrDefault(f => f.name == featureName);
    //     if (featureSet == null) {
    //       Debug.LogError($"Feature set '{featureName}' not found in GoatFeatureBankModule.");
    //       continue;
    //     }
    //
    //     var featureData = new FeatureData {
    //       name = featureSet.name.Replace("_FeatureSet", ""),
    //       agent = agent,
    //       goals = featureSet.goals.Select(goalSo => goalSo.Get(agent)).ToList(),
    //       beliefs = featureSet.beliefs.Select(beliefSo => beliefSo.Get(agent)).ToList(),
    //       actionDatas = featureSet.actionDatas.Select(actionDataSo => actionDataSo.GetAction(agent)).ToList(),
    //       isEnabled = true
    //     };
    //     features.Add(featureData);
    //   }
    //
    //   return features;
    // }

    public List<AgentGoal> GetGoals(IGoapAgent agent, string[] availableFeatures) {
      var goals = new List<AgentGoal>();
      foreach (var featureName in availableFeatures) {
        var featureSet = _featuresSOs.FirstOrDefault(f => f.name == featureName);
        if (featureSet == null) {
          Debug.LogError($"Feature set '{featureName}' not found in GoatFeatureBankModule.");
          continue;
        }
        goals.AddRange(featureSet.goals.Select(goalSo => goalSo.Get(agent)).ToList());
      }

      Debug.Log($"GoatFeatureBankModule:GetGoals:{goals.Count}");
      return goals;
    }
    
    public List<AgentBelief> GetBeliefs(IGoapAgent agent, string[] availableFeatures) {
      var beliefs = new List<AgentBelief>();
      foreach (var featureName in availableFeatures) {
        var featureSet = _featuresSOs.FirstOrDefault(f => f.name == featureName);
        if (featureSet == null) {
          Debug.LogError($"Feature set '{featureName}' not found in GoatFeatureBankModule.");
          continue;
        }
        beliefs.AddRange(featureSet.beliefs.Select(beliefSo => beliefSo.Get(agent)).ToList());
      }
      Debug.Log($"GoatFeatureBankModule:GetBeliefs:{beliefs.Count}");
      return beliefs;
    }
    
    public List<AgentAction> GetActions(IGoapAgent agent, string[] availableFeatures) {
      var actions = new List<AgentAction>();
      foreach (var featureName in availableFeatures) {
        var featureSet = _featuresSOs.FirstOrDefault(f => f.name == featureName);
        if (featureSet == null) {
          Debug.LogError($"Feature set '{featureName}' not found in GoatFeatureBankModule.");
          continue;
        }
        actions.AddRange(featureSet.actionDatas.Select(actionDataSo => actionDataSo.GetAction(agent)).ToList());
      }
      Debug.Log($"GoatFeatureBankModule:GetActions:{actions.Count}");
      return actions;
    }

    public class FeatureData {
      public string name;
      public bool isEnabled = true;
      public IGoapAgent agent;
      public List<AgentGoal> goals;
      public List<AgentBelief> beliefs;
      public List<AgentAction> actionDatas;
    }
  }
}