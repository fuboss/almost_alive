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

    private readonly List<ActorDescription> _allPrefabs = new();
    
    private const float RAYCAST_HEIGHT = 50f;
    private const float RAYCAST_DISTANCE = 300f;
    private static readonly int GROUND_MASK = LayerMask.GetMask("Default");

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
        Debug.LogError($"ActorCreationModule: Prefab not found for actorKey '{actorKey}'");
        return false;
      }

      var spawnPos = CalculateGroundedPosition(position, prefab.gameObject);
      
      actor = _objectResolver.Instantiate(prefab, spawnPos, Quaternion.identity);
      _objectResolver.Inject(actor.gameObject);
      
      var stackData = actor.GetStackData();
      if (stackData != null) stackData.current = count;
      
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
      var boundsOffset = GetBoundsOffsetY(prefab);
      Debug.Log($"spawn with bounds offset {boundsOffset} at groundY {groundY} ({hit.transform.name})", hit.transform);
      
      return new Vector3(targetPos.x, groundY + boundsOffset, targetPos.z);
    }

    /// <summary>
    /// Get Y offset from pivot to bottom of bounds.
    /// </summary>
    private float GetBoundsOffsetY(GameObject prefab) {
      // Try collider first
      var collider = prefab.GetComponentInChildren<Collider>();
      if (collider != null) {
        var bounds = collider.bounds;
        var localMin = prefab.transform.InverseTransformPoint(bounds.min);
        return -localMin.y;
      }

      // Fallback to renderer
      var renderer = prefab.GetComponentInChildren<Renderer>();
      if (renderer != null) {
        var bounds = renderer.bounds;
        var localMin = prefab.transform.InverseTransformPoint(bounds.min);
        return -localMin.y;
      }

      // No bounds info, assume pivot at bottom
      return 0f;
    }

    public void Dispose() {
      _allPrefabs.Clear();
    }
  }
}
