using System;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;

namespace Content.Scripts.Game.Interaction {
  public interface ISelectableActor {
    bool canSelect { get; }
    bool isSelected { get; set; }
    GameObject gameObject { get; }
    
  }
  
  public interface IAgentSelectionModule {
    IGoapAgent GetSelectedAgent();
    void SelectAgent(IGoapAgent agent);
    void SelectAgents(params IGoapAgent[] agents);
    void ClearSelection();
    event Action<IGoapAgent,IGoapAgent> OnSelectionChanged; // (current, prev)
  }
}