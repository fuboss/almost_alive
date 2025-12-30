using System;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class AgentBelief {
    public string name;
    protected Func<bool> _condition = () => false;
    private Func<Vector3> _observedLocation = () => Vector3.zero;
    public Vector3 Location => _observedLocation();

    public virtual bool Evaluate(IGoapAgent agent) {
      return _condition();
    }

    public class Builder {
      private readonly AgentBelief _belief;

      public Builder(string name) {
        _belief = new AgentBelief { name = name };
      }

      public Builder WithCondition(Func<bool> condition) {
        _belief._condition = condition;
        return this;
      }

      public Builder WithLocation(Func<Vector3> observedLocation) {
        _belief._observedLocation = observedLocation;
        return this;
      }

      public AgentBelief Build() {
        return _belief;
      }
    }
  }
}