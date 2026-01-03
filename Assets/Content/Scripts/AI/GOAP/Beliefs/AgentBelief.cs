using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class AgentBelief {
    [ReadOnly] public string name;

    internal Func<bool> _condition = () => false;

    public virtual bool Evaluate(IGoapAgent agent) {
      var evaluate = _condition != null && _condition();
      Debug.Log($"{ToString()} evaluate {evaluate}");
      return evaluate;
    }

    public override string ToString() {
      return $"{GetType().Name}";
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

      public AgentBelief Build() {
        return _belief;
      }
    }

    public virtual AgentBelief Copy(IGoapAgent agent) {
      return new AgentBelief { _condition = _condition, name = name };
    }
  }
}