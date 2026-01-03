using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Stats;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Agent.Descriptors {
  [Serializable]
  public class PerTickStatChange {
    public StatType statType;
    public float delta;
  }

  [Serializable]
  [HideReferenceObjectPicker]
  public class DescriptionData {
    public string[] tags;

    public List<FloatAgentStat.Data> onUseAddStats = new();
    public List<PerTickStatChange> onUseAddStatPerTick = new();
  }
}