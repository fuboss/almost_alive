using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Agent.Memory.Query;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Strategies.Move {
  [Serializable]
  public class WanderUntilStrategy : WanderStrategy {
    [SerializeField] public MemorySearcher targetFromMemory;
    public int lookupForCountInMemory = 1;
    public Func<bool> stopCondition;

    public WanderUntilStrategy() {
    }

    private WanderUntilStrategy(IGoapAgentCore agent) : base(agent, null) {
    }

    public override bool complete {
      get => base.complete || (stopCondition != null && stopCondition.Invoke());
      internal set { }
    }

    public override void OnStart() {
      base.OnStart();
      stopCondition = () => {
        var found = _agent.memory.FindWithTags(targetFromMemory.requiredTags);
        var hasInMemory = found.Length >= lookupForCountInMemory;
        return hasInMemory;
      };
    }

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new WanderUntilStrategy(agent) {
        targetFromMemory = targetFromMemory,
        lookupForCountInMemory = lookupForCountInMemory,
        visitPointsMinMax = visitPointsMinMax,
        navMeshSamples = navMeshSamples,
        defaultWanderRadius = defaultWanderRadius,
        debugWanderAroundCenter = debugWanderAroundCenter
      };
    }
  }
}
