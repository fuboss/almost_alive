using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Game;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP {
  public class ActorCreationModule : IInitializable, IDisposable {
    [Inject] private IObjectResolver _objectResolver;
    public bool IsInitialized { get; private set; }

    public IReadOnlyList<ActorDescription> allPrefabs => _allPrefabs;

    private readonly List<ActorDescription> _allPrefabs = new();

    private const float RAYCAST_HEIGHT = 50f;
    private const float RAYCAST_DISTANCE = 300f;
    private static readonly int GROUND_MASK = LayerMask.GetMask("Terrain");

    void IInitializable.Initialize() {
      Debug.Log("ActorCreationModule:Initialize");
      Addressables.LoadAssetsAsync<GameObject>("Actors").Completed += handle => {
        var actors = handle.Result.Select(g => g.GetComponent<ActorDescription>());
        _allPrefabs.AddRange(actors);
        IsInitialized = true;
      };
    }

    public bool TryGetComponentOnPrefab<T>(string actorKey, out T value) where T : Component {
      value = null;
      var prefab = GetPrefab(actorKey);
      if (prefab != null) {
        value = prefab.GetComponent<T>();
        return value != null;
      }
      Debug.LogError($"ActorCreationModule: Prefab not found for actorKey '{actorKey}'");
      return false;
    }

    public bool TrySpawnActor(string actorKey, Vector3 position, out ActorDescription actor) {
      actor = null;
      var prefab = GetPrefab(actorKey);
      if (prefab == null) {
        Debug.LogError($"ActorCreationModule: Prefab not found for actorKey '{actorKey}'");
        return false;
      }

      actor = _objectResolver.Instantiate(prefab, position, Quaternion.identity);
      _objectResolver.Inject(actor.gameObject);

      return true;
    }

    public bool TrySpawnActorOnGround(string actorKey, Vector3 position, out ActorDescription actor, ushort count = 1) {
      actor = null;
      var prefab = GetPrefab(actorKey);
      if (prefab == null) {
        Debug.LogError($"ActorCreationModule: Prefab not found for actorKey '{actorKey}'");
        return false;
      }

      var spawnPos = CalculateGroundedPosition(position, prefab.gameObject);
      actor = _objectResolver.Instantiate(prefab, spawnPos, Quaternion.identity);
      _objectResolver.Inject(actor.gameObject);

      var stackData = actor.GetStackData();
      if (stackData != null) {
        stackData.current = count;
      }

      return true;
    }

    public ActorDescription GetPrefab(string actorKey) {
      return _allPrefabs.FirstOrDefault(prefab => prefab.actorKey == actorKey);
    }

    /// <summary>
    /// Raycast down from position to find ground, adjust for prefab bounds.
    /// </summary>
    private Vector3 CalculateGroundedPosition(Vector3 targetPos, GameObject prefab) {
      var rayOrigin = new Vector3(targetPos.x, targetPos.y + RAYCAST_HEIGHT, targetPos.z);

      if (!Physics.Raycast(rayOrigin, Vector3.down, out var hit, RAYCAST_DISTANCE, GROUND_MASK)) {
        // No ground found, fallback to original position
        Debug.LogWarning($"[ActorCreation] No ground found at {targetPos}, using original position");
        return targetPos;
      }

      var groundY = hit.point.y;
      var boundsOffset = 0;
      return new Vector3(targetPos.x, groundY + boundsOffset, targetPos.z);
    }


    public void Dispose() {
      _allPrefabs.Clear();
    }
  }
}