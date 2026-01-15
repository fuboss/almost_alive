using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityUtils;

namespace Content.Scripts.AI.GOAP.Agent.Memory {
  [Serializable]
  public class MemorySearcher {
    public SearchMode searchMode = SearchMode.NEAREST;
    [ValueDropdown("GetTags")] public string[] requiredTags;

#if UNITY_EDITOR
    public List<string> GetTags() {
      return GOAPEditorHelper.GetTags();
    }
#endif

    public MemorySnapshot Search(IGoapAgent agent, Func<MemorySnapshot, bool> predicate = null) {
      var targetMem = searchMode switch {
        SearchMode.NEAREST => GetNearest(agent, predicate),
        SearchMode.ANY => agent.memory.GetWithAllTags(requiredTags).Random(),
        _ => GetNearest(agent, predicate) //null
      };

      if (targetMem != null) {
        Debug.Log($"SearchResult: {targetMem.target.name} {targetMem.location}", targetMem.target);
        return targetMem;
      }

      Debug.LogError("MoveStrategy Create: No target found in memory!");
      return null;
    }

    public MemorySnapshot GetNearest(IGoapAgent agent, Func<MemorySnapshot, bool> predicate = null) {
      return GetNearest(agent.memory, agent.position, requiredTags, ms => {
        var valid = ms.target != null;
        if (predicate != null) {
          valid = valid && predicate.Invoke(ms);
        }

        return valid;
      });
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