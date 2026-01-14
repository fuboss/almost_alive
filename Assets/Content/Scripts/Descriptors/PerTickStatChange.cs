using System;
using Content.Scripts.AI.GOAP.Stats;

namespace Content.Scripts.AI.GOAP.Agent.Memory.Descriptors {
  [Serializable]
  public class PerTickStatChange {
    public StatType statType;
    public float delta;
  }
}