using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public abstract class AgentStrategy : IActionStrategy {
    public abstract bool canPerform { get; }
    public abstract bool complete { get; internal set; }
    public abstract IActionStrategy Create(IGoapAgentCore agent);

    public virtual void OnStart() {
    }

    public virtual void OnUpdate(float deltaTime) {
    }

    public virtual void OnStop() {
    }

    public virtual void OnComplete() {
    }

#if UNITY_EDITOR
    public List<string> GetTags() {
      return GOAPEditorHelper.GetTags();
    }
#endif
  }
}
