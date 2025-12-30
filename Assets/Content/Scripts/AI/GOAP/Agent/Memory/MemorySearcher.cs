using System;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  [Serializable]
  public class MemorySearcher {
    public SearchMode searchMode = SearchMode.NEAREST;
    public string[] requiredTags;


    public Func<Vector3> Search(IGoapAgent agent) {
      return () => {
        var targetMem = GetNearest(agent);
        if (targetMem != null) {
          Debug.Log($"SearchResult: {targetMem.target.name} {targetMem.location}", targetMem.target);
          return targetMem.location;
        }

        Debug.LogError("MoveStrategy Create: No target found in memory!");
        return agent.position;
      };
    }

    public MemorySnapshot GetNearest(IGoapAgent agent) {
      return GetNearest(agent.memory, agent.position, requiredTags, ms => ms.target != null);
    }

    public MemorySnapshot GetNearest(AgentMemory memory, Vector3 agentPosition) {
      return GetNearest(memory, agentPosition, requiredTags, ms => ms.target != null);
    }

    public MemorySnapshot GetNearest(AgentMemory memory, Vector3 agentPosition, string[] tags,
      Func<MemorySnapshot, bool> predicate = null) {
      return memory.GetNearest(agentPosition, tags, predicate, false);
    }

    public enum SearchMode {
      NEAREST,
      FARTHEST,
      RANDOM
    }
  }
}