// using System.Collections.Generic;
// using System.Collections.Generic;
// using Content.Scripts.Game;
// using Content.Scripts.Game.Storage;
// using UnityEngine;
//
// namespace Content.Scripts.AI.Camp {
//   /// <summary>
//   /// Per-agent camp data cache. Stores cached storages and provides camp-related API.
//   /// Managed by CampModule.
//   /// </summary>
//   public class AgentCampData {
//     private const float STORAGE_SEARCH_RADIUS = 30f;
//     private const float CACHE_REFRESH_INTERVAL = 2f;
//
//     private CampLocation _camp;
//     private readonly List<StorageActor> _cachedStorages = new();
//     private float _lastStorageCacheTime = -999f;
//
//     public CampLocation camp => _camp;
//     public bool hasCamp => _camp != null;
//     public bool hasSetup => _camp != null && _camp.hasSetup;
//
//     /// <summary>Cached list of storages near camp.</summary>
//     public IReadOnlyList<StorageActor> storages {
//       get {
//         RefreshStorageCacheIfNeeded();
//         return _cachedStorages;
//       }
//     }
//
//     public void SetCamp(CampLocation camp) {
//       _camp = camp;
//       InvalidateCache();
//       if (_camp != null) {
//         RefreshStorageCache();
//       }
//     }
//
//     public void ClearCamp() {
//       _camp = null;
//       InvalidateCache();
//     }
//
//     /// <summary>Invalidate all caches. Call when camp state changes significantly.</summary>
//     public void InvalidateCache() {
//       _cachedStorages.Clear();
//       _lastStorageCacheTime = -999f;
//     }
//
//     /// <summary>Get total count of items with tag across all camp storages.</summary>
//     public int GetResourceCount(string tag) {
//       if (_camp == null) return 0;
//       RefreshStorageCacheIfNeeded();
//       
//       var count = 0;
//       foreach (var storage in _cachedStorages) {
//         if (storage == null) continue;
//         count += storage.GetCountWithTag(tag);
//       }
//       return count;
//     }
//
//     /// <summary>Check if any camp storage has space for given tag.</summary>
//     public bool HasStorageSpaceFor(string tag) {
//       if (_camp == null) return false;
//       RefreshStorageCacheIfNeeded();
//       
//       foreach (var storage in _cachedStorages) {
//         if (storage == null) continue;
//         if (storage.HasSpaceFor(tag)) return true;
//       }
//       return false;
//     }
//
//     /// <summary>Find nearest camp storage that can accept item with tag.</summary>
//     public StorageActor GetNearestStorageFor(Vector3 position, string tag) {
//       if (_camp == null) return null;
//       RefreshStorageCacheIfNeeded();
//
//       StorageActor nearest = null;
//       var minSqrDist = float.MaxValue;
//
//       foreach (var storage in _cachedStorages) {
//         if (storage == null) continue;
//         if (!storage.HasSpaceFor(tag)) continue;
//         
//         var sqrDist = (storage.transform.position - position).sqrMagnitude;
//         if (sqrDist < minSqrDist) {
//           minSqrDist = sqrDist;
//           nearest = storage;
//         }
//       }
//
//       return nearest;
//     }
//
//     /// <summary>Force refresh of cached storages list.</summary>
//     public void RefreshStorageCache() {
//       _cachedStorages.Clear();
//       if (_camp == null) return;
//
//       var campPos = _camp.transform.position;
//       var sqrRadius = STORAGE_SEARCH_RADIUS * STORAGE_SEARCH_RADIUS;
//
//       foreach (var storage in ActorRegistry<StorageActor>.all) {
//         if (storage == null) continue;
//         var sqrDist = (storage.transform.position - campPos).sqrMagnitude;
//         if (sqrDist <= sqrRadius) {
//           _cachedStorages.Add(storage);
//         }
//       }
//
//       _lastStorageCacheTime = Time.time;
//     }
//
//     private void RefreshStorageCacheIfNeeded() {
//       if (Time.time - _lastStorageCacheTime > CACHE_REFRESH_INTERVAL) {
//         RefreshStorageCache();
//       }
//     }
//   }
// }
//
