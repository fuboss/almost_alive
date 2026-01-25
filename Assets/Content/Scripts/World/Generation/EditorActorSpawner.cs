#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Content.Scripts.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Content.Scripts.World.Generation {
  /// <summary>
  /// Spawns actors in editor mode using PrefabUtility.
  /// Handles Addressables loading, caching, and cleanup.
  /// </summary>
  public class EditorActorSpawner : IDisposable {
    private const float RAYCAST_HEIGHT = 50f;
    private const float RAYCAST_DISTANCE = 300f;
    private static readonly int GROUND_MASK = LayerMask.GetMask("Terrain");

    private readonly Dictionary<string, GameObject> _prefabCache = new();
    private readonly Dictionary<string, Transform> _biomeContainers = new();
    private readonly Transform _container;
    
    private IList<GameObject> _loadedActorPrefabs;
    private AsyncOperationHandle<IList<GameObject>> _actorsHandle;

    public int SpawnedCount { get; private set; }

    public EditorActorSpawner(Transform container) {
      _container = container;
    }

    /// <summary>
    /// Spawn a single actor from spawn data.
    /// </summary>
    public bool SpawnActor(WorldSpawnData data) {
      if (_container == null) return false;

      var prefab = LoadPrefabByKey(data.actorKey);
      if (prefab == null) return false;

      // Get or create biome container for hierarchy
      if (!_biomeContainers.TryGetValue(data.biomeId, out var biomeContainer)) {
        var biomeGo = new GameObject($"[{data.biomeId}]");
        biomeGo.transform.SetParent(_container, false);
        biomeContainer = biomeGo.transform;
        _biomeContainers[data.biomeId] = biomeContainer;
        Undo.RegisterCreatedObjectUndo(biomeGo, "Create Biome Container");
      }

      var groundedPos = CalculateGroundedPosition(data.position);
      var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, biomeContainer);
      if (instance == null) return false;

      instance.transform.position = groundedPos;
      instance.transform.rotation = Quaternion.Euler(0, data.rotation, 0);
      instance.transform.localScale = Vector3.one * data.scale;

      Undo.RegisterCreatedObjectUndo(instance, "Spawn Actor");
      SpawnedCount++;
      return true;
    }

    private GameObject LoadPrefabByKey(string actorKey) {
      if (string.IsNullOrWhiteSpace(actorKey)) return null;
      if (_prefabCache.TryGetValue(actorKey, out var cached)) return cached;

      // Load all actor prefabs once and cache
      if (_loadedActorPrefabs == null) {
        _actorsHandle = Addressables.LoadAssetsAsync<GameObject>("Actors", null);
        _loadedActorPrefabs = _actorsHandle.WaitForCompletion();
      }

      foreach (var prefab in _loadedActorPrefabs) {
        var actor = prefab.GetComponent<ActorDescription>();
        if (actor != null && actor.actorKey == actorKey) {
          _prefabCache[actorKey] = prefab;
          return prefab;
        }
      }
      return null;
    }

    private static Vector3 CalculateGroundedPosition(Vector3 targetPos) {
      var rayOrigin = new Vector3(targetPos.x, targetPos.y + RAYCAST_HEIGHT, targetPos.z);
      return Physics.Raycast(rayOrigin, Vector3.down, out var hit, RAYCAST_DISTANCE, GROUND_MASK)
        ? hit.point
        : targetPos;
    }

    public void Dispose() {
      if (_loadedActorPrefabs != null) {
        Addressables.Release(_actorsHandle);
        _loadedActorPrefabs = null;
      }
      _prefabCache.Clear();
      _biomeContainers.Clear();
    }
  }
}
#endif

