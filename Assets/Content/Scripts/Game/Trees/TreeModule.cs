using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Game.Trees {
  public class TreeModule {
    [Inject] private ActorDestructionModule _actorDestruction;
    [Inject] private ActorCreationModule _creationModule;

    public void ChopDownTree(ChoppingProgress choppingProgress, IGoapAgentCore byAgent) {
      if (choppingProgress == null || choppingProgress.actor == null) return;
      if (!choppingProgress.isComplete) return;

      var treeDef = choppingProgress.actor.GetDefinition<TreeTag>();
      if (treeDef != null) {
        if (treeDef.woodYield > 0 && !string.IsNullOrEmpty(treeDef.woodActorID)) {
          SpawnYieldWood(choppingProgress, byAgent, treeDef.woodYield, treeDef.woodActorID);
        }
      }

      Debug.Log($"[TreeModule] Tree {choppingProgress.actor.name} chopped down by agent");
      _actorDestruction.DestroyActor(choppingProgress.actor);
    }

    private void SpawnYieldWood(ChoppingProgress choppingProgress, IGoapAgentCore byAgent, int yield, string actorID) {
      var shift = Vector3.zero;
      for (int i = 0; i < yield; i++) {
        if (!_creationModule.TrySpawnActorOnGround(actorID, choppingProgress.transform.position + shift,
              out var woodActor)) continue;
        shift = NextShift();
        byAgent.agentBrain.TryRemember(woodActor, out var _);
      }
    }

    private static Vector3 NextShift() {
      var shift = Random.onUnitSphere;
      shift.y = Mathf.Abs(shift.y);
      shift += Vector3.up * 0.66f;
      return shift;
    }
  }
}