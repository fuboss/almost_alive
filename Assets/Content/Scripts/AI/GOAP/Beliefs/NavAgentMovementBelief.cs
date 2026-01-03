using System;
using Content.Scripts.AI.GOAP.Agent;

namespace Content.Scripts.AI.GOAP.Beliefs {
  [Serializable]
  public class NavAgentMovementBelief : AgentBelief {
    public bool isMoving;

    public override bool Evaluate(IGoapAgent agent) {
      _condition = () => agent.isMoving == isMoving;

      return base.Evaluate(agent);
    }

    public override AgentBelief Copy(IGoapAgent agent) {
      var copy = new NavAgentMovementBelief() {
        _condition = _condition,
        name = name,
        isMoving = isMoving
      };
      return copy;
    }
  }
}