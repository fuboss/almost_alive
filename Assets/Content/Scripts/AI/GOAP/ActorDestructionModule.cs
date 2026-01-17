using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP {
  public class ActorDestructionModule : IInitializable {
    [Inject] private IObjectResolver _objectResolver;
    [Inject] private ActorCreationModule _creationModule;

    void IInitializable.Initialize() {
    }

    public void DestroyActor(ActorDescription actor, IGoapAgent byAgent = null) {
      if (actor == null) {
        Debug.LogError("ActorDestructionModule: DestroyActor: actor is null");
        return;
      }

      foreach (var partActor in SpawnPartsIfNeeded(actor)) {
        Debug.Log($"{actor.name} Spawned a: {partActor.name}");
        byAgent?.agentBrain.TryRemember(partActor);
      }

      Object.Destroy(actor.gameObject);
    }

    private List<ActorDescription> SpawnPartsIfNeeded(ActorDescription actor) {
      var spawnedParts = new List<ActorDescription>();
      var parts = actor.GetComponent<ActorPartsDescription>();
      if (parts == null || parts.parts.Count == 0) return spawnedParts;

      var shift = Vector3.zero;
      foreach (var (actorKey, count) in parts.parts) {
        if (!_creationModule.TrySpawnActor(actorKey, actor.transform.position + shift, out var partActor,
              count)) continue;

        spawnedParts.Add(partActor);
        shift = NextShift();
      }

      return spawnedParts;
    }

    private static Vector3 NextShift() {
      var shift = Random.onUnitSphere;
      shift.y = Mathf.Abs(shift.y);
      shift += Vector3.up * 0.66f;
      return shift;
    }
  }
}