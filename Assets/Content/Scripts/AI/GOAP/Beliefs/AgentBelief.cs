using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class AgentBelief {
    [ReadOnly] public string name;

    internal Func<bool> _condition = () => false;
    [ShowInInspector, ReadOnly] protected bool _lastEvaluation;

    public bool lastEvaluation => _lastEvaluation;

    public virtual bool Evaluate(IGoapAgent agent) {
      var evaluate = _condition != null && _condition();
      _lastEvaluation = evaluate;
      // Debug.Log($"[EVALUATE]{ToString()} = {evaluate}");
      return evaluate;
    }

    public override string ToString() {
      return $"{GetType().Name}";
    }

    public virtual AgentBelief Copy(IGoapAgent agent) {
      return new AgentBelief { _condition = _condition, name = name };
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
        _belief._condition = condition;
        return this;
      }

      public AgentBelief Build() {
        return _belief;
      }
    }
  }
}