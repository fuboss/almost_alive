using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class AgentBelief {
    [ReadOnly] public string name;

    internal Func<bool> condition = () => false;
    [ShowInInspector, ReadOnly] protected bool _lastEvaluation;

    public bool lastEvaluation => _lastEvaluation;

    public virtual bool Evaluate(IGoapAgentCore agent) {
      condition = GetCondition(agent);
      var evaluate = condition != null && condition();
      _lastEvaluation = evaluate;
      return evaluate;
    }

    protected virtual Func<bool> GetCondition(IGoapAgentCore agent) {
      return condition ?? (() => false);
    }

    public override string ToString() {
      return $"{GetType().Name}";
    }

    public virtual AgentBelief Copy() {
      return new AgentBelief { condition = condition, name = name };
    }

    public virtual string GetPresenterString() {
      return name;
    }

#if UNITY_EDITOR
    public List<string> GetTags() {
      return GOAPEditorHelper.GetTags();
    }
#endif

    public class Builder {
      private readonly AgentBelief _belief;

      public Builder(string name) {
        _belief = new AgentBelief { name = name };
      }

      public Builder WithCondition(Func<bool> condition) {
        _belief.condition = condition;
        return this;
      }

      public AgentBelief Build() {
        return _belief;
      }
    }
  }
}
