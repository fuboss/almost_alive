using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Beliefs;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Planning {
  public class PlannerNode {
    public PlannerNode(PlannerNode parent, AgentAction action, HashSet<AgentBelief> effects, float cost, float benefit = 0f) {
      Parent = parent;
      Action = action;
      RequiredEffects = new HashSet<AgentBelief>(effects);
      Leaves = new List<PlannerNode>();
      Cost = cost;
      Benefit = benefit;
    }

    public PlannerNode Parent { get; }
    public AgentAction Action { get; }
    public HashSet<AgentBelief> RequiredEffects { get; }
    public List<PlannerNode> Leaves { get; }
    public float Cost { get; }
    public float Benefit { get; }

    /// <summary>
    /// Score for comparing plans. Higher = better plan.
    /// </summary>
    public float Score => Benefit / Mathf.Max(Cost, 0.1f);

    public bool IsLeafDead => Leaves.Count == 0 && Action == null;
  }
}
