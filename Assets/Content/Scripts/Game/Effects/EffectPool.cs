using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.Game.Effects {
  public class EffectPool {
    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
    private readonly Transform _poolRoot;

    public EffectPool(Transform poolRoot = null) {
      _poolRoot = poolRoot;
    }

    public GameObject Get(GameObject prefab) {
      if (prefab == null) return null;

      if (_pools.TryGetValue(prefab, out var queue) && queue.Count > 0) {
        var instance = queue.Dequeue();
        if (instance != null) {
          instance.SetActive(true);
          return instance;
        }
      }

      var newInstance = Object.Instantiate(prefab);
      return newInstance;
    }

    public void Return(GameObject prefab, GameObject instance) {
      if (instance == null) return;

      if (prefab == null) {
        Object.Destroy(instance);
        return;
      }

      instance.SetActive(false);
      if (_poolRoot != null) {
        instance.transform.SetParent(_poolRoot);
      }

      if (!_pools.TryGetValue(prefab, out var queue)) {
        queue = new Queue<GameObject>();
        _pools[prefab] = queue;
      }

      queue.Enqueue(instance);
    }

    public void Clear() {
      foreach (var kvp in _pools) {
        while (kvp.Value.Count > 0) {
          var instance = kvp.Value.Dequeue();
          if (instance != null) {
            Object.Destroy(instance);
          }
        }
      }
      _pools.Clear();
    }

    public void Prewarm(GameObject prefab, int count) {
      if (prefab == null || count <= 0) return;

      if (!_pools.TryGetValue(prefab, out var queue)) {
        queue = new Queue<GameObject>();
        _pools[prefab] = queue;
      }

      for (int i = 0; i < count; i++) {
        var instance = Object.Instantiate(prefab);
        instance.SetActive(false);
        if (_poolRoot != null) {
          instance.transform.SetParent(_poolRoot);
        }
        queue.Enqueue(instance);
      }
    }
  }
}
