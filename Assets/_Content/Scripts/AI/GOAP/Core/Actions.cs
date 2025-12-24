using System.Collections.Generic;
using _Content.Scripts.AI.GOAP.Strategies;

namespace _Content.Scripts.AI.GOAP.Core {
  public class AgentAction {
    private IActionStrategy _strategy;

    private AgentAction(string name) {
      Name = name;
    }

    public string Name { get; }
    public float Cost { get; private set; }

    public HashSet<AgentBelief> Preconditions { get; } = new();
    public HashSet<AgentBelief> Effects { get; } = new();
    public bool Complete => _strategy.Complete;

    public void Start() {
      _strategy.Start();
    }

    public void Update(float deltaTime) {
      // Check if the action can be performed and update the strategy
      if (_strategy.CanPerform) _strategy.Update(deltaTime);

      // Bail out if the strategy is still executing
      if (!_strategy.Complete) return;

      // Apply effects
      foreach (var effect in Effects) effect.Evaluate();
    }

    public void Stop() {
      _strategy.Stop();
    }

    public class Builder {
      private readonly AgentAction _action;

      public Builder(string name) {
        _action = new AgentAction(name) {
          Cost = 1
        };
      }

      public Builder WithCost(float cost) {
        _action.Cost = cost;
        return this;
      }

      public Builder WithStrategy(IActionStrategy strategy) {
        _action._strategy = strategy;
        return this;
      }

      public Builder AddPrecondition(AgentBelief precondition) {
        _action.Preconditions.Add(precondition);
        return this;
      }

      public Builder AddEffect(AgentBelief effect) {
        _action.Effects.Add(effect);
        return this;
      }

      public AgentAction Build() {
        return _action;
      }
    }
  }
}