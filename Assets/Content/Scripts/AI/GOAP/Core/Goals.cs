using System.Collections.Generic;

namespace Content.Scripts.AI.GOAP.Core {
  public class AgentGoal {
    private AgentGoal(string name) {
      Name = name;
    }

    public string Name { get; }
    public float Priority { get; private set; }
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

      public AgentGoal Build() {
        return _goal;
      }
    }
  }
}