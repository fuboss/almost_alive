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
      Debug.Log($"GoatFeatureBankModule:{_featuresSOs?.Count.ToString() ?? "NULL"}");
    }

    public static GoatFeatureBankModule GetFromResources() {
      var goalSOs = Resources.LoadAll<GoatFeatureSO>("GOAP").ToList();
      return new GoatFeatureBankModule(goalSOs);
    }

    public List<GoalTemplate> GetGoals(string[] availableFeatures) {
      var goals = new List<GoalTemplate>();
      foreach (var featureName in availableFeatures) {
        var featureSet = _featuresSOs.FirstOrDefault(f => f.name == featureName);
        if (featureSet == null) {
          Debug.LogError($"Feature set '{featureName}' not found in GoatFeatureBankModule.");
          continue;
        }

        goals.AddRange(featureSet.goals.Select(g=>g.template));
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

        beliefs.AddRange(featureSet.beliefs.Select(beliefSo => beliefSo.Get()).ToList());
        beliefs.AddRange(featureSet.compositeBeliefs.SelectMany(beliefSo => beliefSo.Get()).ToList());
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
  }
}