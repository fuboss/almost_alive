using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Actions {
  [Serializable]
  public class AgentActionData {
    public string name;
    [Tooltip("Resource cost to execute this action")]
    public int cost = 1;
    
    [Tooltip("Expected benefit/utility from this action. Higher = more valuable outcome")]
    [MinValue(0.1f)]
    public float benefit = 1f;
    
    [SerializeReference] public IActionStrategy strategy;
    [ValueDropdown("GetEffectNames")] public List<string> preconditions;
    [ValueDropdown("GetEffectNames")] public List<string> effects;

    /// <summary>
    /// Score for planning. Higher is better.
    /// </summary>
    public float Score => benefit / Mathf.Max(cost, 0.1f);

#if UNITY_EDITOR
    public List<string> GetEffectNames() {
      return GOAPEditorHelper.GetBeliefsNames();
    }
#endif
  }
}
