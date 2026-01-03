using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityUtils;

namespace Content.Scripts.AI.GOAP.Agent {
  [Serializable]
  public class MemorySearcher {
    public SearchMode searchMode = SearchMode.NEAREST;
    [ValueDropdown("GetTags")] public string[] requiredTags;
#if UNITY_EDITOR
    public List<string> GetTags() {
      return GOAPEditorHelper.GetTags();
    }
#endif

    public Func<Vector3> Search(IGoapAgent agent) {
      return SearchImp;

      Vector3 SearchImp() {
        var targetMem = searchMode switch {
          SearchMode.NEAREST => GetNearest(agent),
          SearchMode.ANY => agent.memory.GetWithAllTags(requiredTags).Random(),
          _ => GetNearest(agent) //null
        };

        if (targetMem != null) {
          Debug.Log($"SearchResult: {targetMem.target.name} {targetMem.location}", targetMem.target);
          return targetMem.location;
        }

        Debug.LogError("MoveStrategy Create: No target found in memory!");
        return agent.position;
      }
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
      ANY
    }
  }
}