using System;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs.Common {
  [Serializable]
  public class NavAgentMovementBelief : AgentBelief {
    public bool isMoving;

    public override bool Evaluate(IGoapAgent agent) {
      condition = () => agent.isMoving == isMoving;

      return base.Evaluate(agent);
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