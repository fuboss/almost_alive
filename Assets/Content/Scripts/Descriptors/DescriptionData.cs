using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent.Memory.Descriptors {
  [Serializable]
  [HideReferenceObjectPicker]
  public class DescriptionData {
    [ValueDropdown("GetNames")] public string[] tags;
    public float rememberDuration = 300f;

    public List<FloatAgentStat.Data> onUseAddStats = new();
    public List<PerTickStatChange> onUseAddStatPerTick = new();
    
#if UNITY_EDITOR
    public List<string> GetNames() {
      return GOAPEditorHelper.GetTags();
    }
#endif
  }
}