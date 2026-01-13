using System;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Agent.Memory.Query;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies.Move {
  [Serializable]
  public class WanderUntilStrategy : WanderStrategy {
    [SerializeField] public MemorySearcher targetFromMemory;
    public int lookupForCountInMemory = 1;
    public Func<bool> stopCondition;

    public override bool complete {
      get => base.complete || (stopCondition != null && stopCondition.Invoke());
      internal set { }
    }

    public override void OnStart() {
      base.OnStart();
      stopCondition = () => _agent.memory.FindWithTags(Tag.FOOD).Length >= lookupForCountInMemory;
    }
  }
}