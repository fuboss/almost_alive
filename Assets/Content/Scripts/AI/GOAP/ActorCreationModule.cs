using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Game;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP {
  public class ActorCreationModule : IInitializable,IDisposable {
    [Inject] private IObjectResolver _objectResolver;

    private readonly List<ActorDescription> _allPrefabs = new();

    void IInitializable.Initialize() {
      Debug.Log("ActorCreationModule:Initialize");
      Addressables.LoadAssetsAsync<GameObject>("Actors").Completed += handle => {
        var actors = handle.Result.Select(g => g.GetComponent<ActorDescription>());
        _allPrefabs.AddRange(actors);
      };
    }

    public bool TrySpawnActor(string actorKey, Vector3 position, out ActorDescription actor, ushort count = 1) {
      actor = null;
      var prefab = GetPrefab(actorKey);
      if (prefab == null) {
        Debug.LogError($"ActorCreationModule: TrySpawnActor: Prefab not found for actorKey '{actorKey}'");
        return false;
      }

      actor = _objectResolver.Instantiate(prefab, position, Quaternion.identity);
      _objectResolver.Inject(actor.gameObject);
      actor.GetStackData().current = count;
      return true;
    }

    private ActorDescription GetPrefab(string actorKey) {
      return _allPrefabs.FirstOrDefault(prefab => prefab.actorKey == actorKey);
      // return Resources
      //   .LoadAll<ActorDescription>($"PrefabsActors/")
      //   .FirstOrDefault(a => a != null && a.name == id);
    }

    public void Dispose() {
      _allPrefabs.Clear();
    }
  }
}