using System;
using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Actions {
  [Serializable]
  public class AgentActionData {
    public string name;
    public int cost;
    [SerializeReference] public IActionStrategy strategy;
    public List<string> preconditions;
    public List<string> effects;
  }
}