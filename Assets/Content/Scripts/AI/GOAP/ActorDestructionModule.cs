using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP {
  public class ActorDestructionModule : IInitializable {
    [Inject] private ActorCreationModule _creationModule;

    void IInitializable.Initialize() {
    }

    public void DestroyActor(ActorDescription actor, IGoapAgent byAgent = null) {
      if (actor == null) {
        Debug.LogError("ActorDestructionModule: DestroyActor: actor is null");
        return;
      }
      Object.Destroy(actor.gameObject);
    }

    private static Vector3 NextShift() {
      var shift = Random.onUnitSphere;
      shift.y = Mathf.Abs(shift.y);
      shift += Vector3.up * 0.66f;
      return shift;
    }
  }
}