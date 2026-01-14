using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Beliefs {
  public abstract class CompositeBeliefSO : SerializedScriptableObject {
    public abstract IEnumerable<AgentBelief> Get();
  }

  public abstract class CompositeBeliefSO<TBelief> : CompositeBeliefSO
    where TBelief : AgentBelief {
    [SerializeReference] public List<TBelief> beliefs;
    
    public override IEnumerable<AgentBelief> Get() {
      return beliefs;
    }

    [Button]
    public void CreateForEachTag() {
      beliefs.Clear();
      foreach (var tag in Tag.ALL_TAGS) {
        beliefs.Add(CreateBeliefForTag(tag));
      }
    }

    protected abstract TBelief CreateBeliefForTag(string tag);
  }
}