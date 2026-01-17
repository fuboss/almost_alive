using System;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Common {
  [Serializable, TypeInfoBox("True when agent's movement state matches (moving or stopped).")]
  public class NavAgentMovementBelief : AgentBelief {
    public bool isMoving;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => agent.isMoving == isMoving;
    }

    public override AgentBelief Copy() {
      return new NavAgentMovementBelief {
        condition = condition,
        name = name,
        isMoving = isMoving
      };
    }
  }
}
