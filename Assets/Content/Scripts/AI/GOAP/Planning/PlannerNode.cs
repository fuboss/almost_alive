using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Beliefs;

namespace Content.Scripts.AI.GOAP.Planning {
  public class PlannerNode {
    public PlannerNode(PlannerNode parent, AgentAction action, HashSet<AgentBelief> effects, float cost) {
      Parent = parent;
      Action = action;
      RequiredEffects = new HashSet<AgentBelief>(effects);
      Leaves = new List<PlannerNode>();
      Cost = cost;
    }

    public PlannerNode Parent { get; }
    public AgentAction Action { get; }
    public HashSet<AgentBelief> RequiredEffects { get; }
    public List<PlannerNode> Leaves { get; }
    public float Cost { get; }

    public bool IsLeafDead => Leaves.Count == 0 && Action == null;
  }
}