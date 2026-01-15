using System;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs.Common {
  [Serializable]
  public class NavAgentMovementBelief : AgentBelief {
    public bool isMoving;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => agent.isMoving == isMoving;
    }

    public override AgentBelief Copy() {
      var copy = new NavAgentMovementBelief() {
        condition = condition,
        name = name,
        isMoving = isMoving
      };
      return copy;
    }
  }
}