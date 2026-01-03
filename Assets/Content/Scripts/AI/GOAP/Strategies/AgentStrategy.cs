using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public abstract class AgentStrategy : IActionStrategy {
    public abstract bool canPerform { get; }
    public abstract bool complete { get; internal set; }
    public abstract IActionStrategy Create(IGoapAgent agent);

    public virtual void OnStart() {
      // noop
    }

    public virtual void OnUpdate(float deltaTime) {
      // noop
    }

    public virtual void OnStop() {
      // noop
    }
    
#if UNITY_EDITOR
    public List<string> GetTags() {
      return GOAPEditorHelper.GetTags();
    }
#endif
  }
}