using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;
using Random = UnityEngine.Random;

namespace Content.Scripts.AI.Camp {
  public class CampModule : IInitializable, IDisposable {
    [Inject] private IObjectResolver _resolver;

    private readonly List<CampSetup> _setupPrefabs = new();
    private bool _loaded;

    void IInitializable.Initialize() {
      Addressables.LoadAssetsAsync<GameObject>("CampSetups").Completed += handle => {
        foreach (var go in handle.Result) {
          if (go.TryGetComponent<CampSetup>(out var setup))
            _setupPrefabs.Add(setup);
        }
        _loaded = true;
        Debug.Log($"[CampModule] Loaded {_setupPrefabs.Count} camp setups");
      };
    }

    public bool isReady => _loaded && _setupPrefabs.Count > 0;

    /// <summary>Instantiates random CampSetup at given location.</summary>
    public CampSetup InstantiateRandomSetup(CampLocation location) {
      if (!isReady) {
        Debug.LogError("[CampModule] Not ready or no setups loaded");
        return null;
      }
      
      var prefab = _setupPrefabs[Random.Range(0, _setupPrefabs.Count)];
      var instance = _resolver.Instantiate(prefab, location.transform.position, Quaternion.identity);
      location.AssignSetup(instance);
      return instance;
    }

    /// <summary>Instantiates specific CampSetup by index.</summary>
    public CampSetup InstantiateSetup(CampLocation location, int index) {
      if (!isReady || index < 0 || index >= _setupPrefabs.Count) return null;
      
      var prefab = _setupPrefabs[index];
      var instance = _resolver.Instantiate(prefab, location.transform.position, Quaternion.identity);
      location.AssignSetup(instance);
      return instance;
    }

    void IDisposable.Dispose() {
      _setupPrefabs.Clear();
    }
  }
}
