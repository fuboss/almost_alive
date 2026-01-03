using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Beliefs;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Actions {
  [Serializable]
  public class AgentAction {
    [ShowInInspector] private AgentActionData _data;
    public float cost = 1f;

    private AgentAction(string name) {
      this.name = name;
      _data = new AgentActionData {
        name = name,
        effects = new List<string>(),
        preconditions = new List<string>()
      };
    }

    [ShowInInspector] public string name { get; }
    public bool complete => _data.strategy.complete;

    public bool AreAllPreconditionsMet(IGoapAgent agent)
      => preconditions.All(precondition => {
        var result = precondition.Evaluate(agent);
        if (!result) Debug.LogWarning($"{precondition.name} failed", agent.body);
        return result;
      });

    [ShowInInspector] public HashSet<AgentBelief> effects { get; set; }
    [ShowInInspector] public HashSet<AgentBelief> preconditions { get; set; }
    public IGoapAgent agent { get; set; }

    public void OnStart() {
      // Debug.Log($"{name} with strategy {_data.strategy.GetType().Name} starting.");
      _data.strategy.OnStart();
    }

    public void OnUpdate(float deltaTime) {
      // Check if the action can be performed and update the strategy
      if (_data.strategy.canPerform) _data.strategy.OnUpdate(deltaTime);

      // Bail out if the strategy is still executing
      if (!_data.strategy.complete) return;

      OnComplete();
    }

    private void OnComplete() {
      // Apply effects
      foreach (var effect in effects) effect.Evaluate(agent);
    }

    public void OnStop() {
      // Debug.Log($"{name} with strategy {_data.strategy.GetType().Name} stopped.");
      _data.strategy.OnStop();
    }

    public class Builder {
      private readonly AgentAction _action;

      public Builder(string name) {
        _action = new AgentAction(name) {
          cost = 1,
          effects = new HashSet<AgentBelief>(),
          preconditions = new HashSet<AgentBelief>()
        };
      }

      public Builder WithCost(int cost) {
        _action.cost = cost;
        _action._data.cost = cost;
        return this;
      }

      public Builder WithStrategy([NotNull] IActionStrategy strategy) {
        _action._data.strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        return this;
      }

      public Builder AddPrecondition(AgentBelief precondition) {
        if (precondition != null) {
          _action.preconditions.Add(precondition);
          _action._data.preconditions.Add(precondition.name);
        }

        return this;
      }

      public Builder AddPreconditions(IEnumerable<AgentBelief> preconditions) {
        foreach (var precondition in preconditions) {
          AddPrecondition(precondition);
        }

        return this;
      }

      public Builder AddEffect(AgentBelief effect) {
        if (effect != null) {
          _action.effects.Add(effect);
          _action._data.effects.Add(effect.name);
        }

        return this;
      }

      public Builder AddEffects(IEnumerable<AgentBelief> effects) {
        foreach (var effect in effects) {
          AddEffect(effect);
        }

        return this;
      }

      public AgentAction Build() {
        return _action;
      }
    }
  }
}