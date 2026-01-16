using Content.Scripts.Game;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP {
  public class ActorDestructionModule : IInitializable {
    [Inject] private IObjectResolver _objectResolver;
    [Inject] private ActorCreationModule _creationModule;

    void IInitializable.Initialize() {
      Debug.Log("ActorDestructionModule:Initialize");
    }

    public void DestroyActor(ActorDescription actor) {
      if (actor == null) {
        Debug.LogError("ActorDestructionModule: DestroyActor: actor is null");
        return;
      }

      SpawnPartsIfNeeded(actor);

      Object.Destroy(actor.gameObject);
    }

    private void SpawnPartsIfNeeded(ActorDescription actor) {
      var parts = actor.GetComponent<ActorPartsDescription>();
      if (parts == null || parts.parts.Count == 0) return;

      var shift = NextShift();
      foreach (var part in parts.parts) {
        if (!_creationModule.TrySpawnActor(part.Key, actor.transform.position + shift, out var partActor,
              part.Value)) continue;

        shift = NextShift();
      }
    }

    private static Vector3 NextShift() {
      var shift = Random.onUnitSphere;
      shift.y = Mathf.Abs(shift.y);
      shift += Vector3.up * 1.1f;
      return shift;
    }
  }
}