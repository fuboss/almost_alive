using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP {
  public class GoapFeatureBankModule : IInitializable {
    [Inject] private IObjectResolver _objectResolver;
    
    private List<GoapFeatureSO> _featuresSOs;

    void IInitializable.Initialize() {
      _featuresSOs = new List<GoapFeatureSO>(Resources.LoadAll<GoapFeatureSO>("GOAP"));
    }

    public List<GoalTemplate> GetGoals(string[] availableFeatures) {
      var goals = new List<GoalTemplate>();
      foreach (var featureName in availableFeatures) {
        var featureSet = _featuresSOs.FirstOrDefault(f => f.name == featureName);
        if (featureSet == null) {
          Debug.LogError($"Feature set '{featureName}' not found in GoatFeatureBankModule.");
          continue;
        }

        goals.AddRange(featureSet.goals.Select(g => g.template));
      }

      return goals;
    }

    public List<AgentBelief> GetBeliefs(string[] availableFeatures) {
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

      return beliefs;
    }

    public List<AgentAction> GetActions(IGoapAgentCore agent, string[] availableFeatures) {
      var actions = new List<AgentAction>();

      foreach (var featureName in availableFeatures) {
        var featureSet = _featuresSOs.FirstOrDefault(f => f.name == featureName);
        if (featureSet == null) {
          Debug.LogError($"Feature set '{featureName}' not found in GoatFeatureBankModule.");
          continue;
        }

        actions.AddRange(featureSet.actionDatas.Select(actionDataSo => {
          if (actionDataSo == null) {
            Debug.LogError("actionDataSo == null");
            return null;
          }
          var action = actionDataSo.GetAction(agent, _objectResolver);
          return action;
        }).ToList());
      }

      return actions;
    }
  }
}
