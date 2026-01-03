using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Actions {
  [Serializable]
  public class AgentActionData {
    public string name;
    public int cost;
    [SerializeReference] public IActionStrategy strategy;
    [ValueDropdown("GetEffectNames")] public List<string> preconditions;
    [ValueDropdown("GetEffectNames")] public List<string> effects;

#if UNITY_EDITOR
    public List<string> GetEffectNames() {
      return GOAPEditorHelper.GetBeliefsNames();
    }
#endif
  }
}