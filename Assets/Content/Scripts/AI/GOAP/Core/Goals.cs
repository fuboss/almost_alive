using System;
using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Core {

  [Serializable]
  public class AgentGoal {
    [SerializeField] private string _name;
    [SerializeField] private float _priority;

    private AgentGoal(string name) {
      Name = name;
    }

    public string Name {
      get => _name;
      private set => _name = value;
    }

    public float Priority {
      get => _priority;
      private set => _priority = value;
    }

    public HashSet<AgentBelief> DesiredEffects { get; } = new();

    public class Builder {
      private readonly AgentGoal _goal;

      public Builder(string name) {
        _goal = new AgentGoal(name);
      }

      public Builder WithPriority(float priority) {
        _goal.Priority = priority;
        return this;
      }

      public Builder WithDesiredEffect(AgentBelief effect) {
        _goal.DesiredEffects.Add(effect);
        return this;
      }

      public Builder WithDesiredEffects(IEnumerable<AgentBelief> effect) {
        _goal.DesiredEffects.AddRange(effect);
        return this;
      }

      public AgentGoal Build() {
        if (_goal.DesiredEffects.Count == 0) {
          Debug.LogError($"{_goal.Name} has no desired effects defined!");
        }

        return _goal;
      }
    }
  }
}