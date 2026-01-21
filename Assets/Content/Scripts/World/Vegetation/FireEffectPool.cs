using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.World.Vegetation {
  /// <summary>
  /// Object pool for fire particle effects.
  /// </summary>
  public class FireEffectPool {
    private readonly GameObject _prefab;
    private readonly Transform _container;
    private readonly Stack<ParticleSystem> _pool = new();
    private readonly Dictionary<Vector2Int, ParticleSystem> _activeEffects = new();

    public int ActiveCount => _activeEffects.Count;
    public int PooledCount => _pool.Count;

    public FireEffectPool(GameObject prefab, Transform parent, int preloadCount = 10) {
      _prefab = prefab;
      
      // Create container for effects
      var containerGO = new GameObject("[FireEffects_Pool]");
      containerGO.transform.SetParent(parent);
      _container = containerGO.transform;
      
      // Preload pool
      for (var i = 0; i < preloadCount; i++) {
        var instance = CreateInstance();
        instance.gameObject.SetActive(false);
        _pool.Push(instance);
      }
      
      Debug.Log($"[FireEffectPool] Initialized with {preloadCount} preloaded effects");
    }

    /// <summary>
    /// Spawn fire effect at grid cell. Returns true if spawned.
    /// </summary>
    public bool SpawnAt(Vector2Int gridCoord, Vector3 worldPos) {
      // Already has effect at this cell
      if (_activeEffects.ContainsKey(gridCoord)) {
        return false;
      }
      
      ParticleSystem effect;
      
      if (_pool.Count > 0) {
        effect = _pool.Pop();
        effect.gameObject.SetActive(true);
      } else {
        effect = CreateInstance();
      }
      
      effect.transform.position = worldPos;
      effect.Clear();
      effect.Play();
      
      _activeEffects[gridCoord] = effect;
      return true;
    }

    /// <summary>
    /// Remove fire effect at grid cell.
    /// </summary>
    public void RemoveAt(Vector2Int gridCoord) {
      if (!_activeEffects.TryGetValue(gridCoord, out var effect)) {
        return;
      }
      
      _activeEffects.Remove(gridCoord);
      
      // Stop emitting but let existing particles fade
      effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
      
      // Return to pool after delay
      ReturnToPoolDelayed(effect);
    }

    /// <summary>
    /// Get all active fire positions.
    /// </summary>
    public IReadOnlyCollection<Vector2Int> GetActivePositions() {
      return _activeEffects.Keys;
    }

    /// <summary>
    /// Check if cell has active fire effect.
    /// </summary>
    public bool HasEffectAt(Vector2Int gridCoord) {
      return _activeEffects.ContainsKey(gridCoord);
    }

    /// <summary>
    /// Clear all active effects immediately.
    /// </summary>
    public void ClearAll() {
      foreach (var kvp in _activeEffects) {
        var effect = kvp.Value;
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.gameObject.SetActive(false);
        _pool.Push(effect);
      }
      _activeEffects.Clear();
    }

    /// <summary>
    /// Destroy pool and all instances.
    /// </summary>
    public void Destroy() {
      ClearAll();
      
      while (_pool.Count > 0) {
        var effect = _pool.Pop();
        if (effect != null) {
          Object.Destroy(effect.gameObject);
        }
      }
      
      if (_container != null) {
        Object.Destroy(_container.gameObject);
      }
    }

    private ParticleSystem CreateInstance() {
      var go = Object.Instantiate(_prefab, _container);
      go.name = $"FireEffect_{_pool.Count + _activeEffects.Count}";
      
      var ps = go.GetComponent<ParticleSystem>();
      if (ps == null) {
        ps = go.GetComponentInChildren<ParticleSystem>();
      }
      
      if (ps == null) {
        Debug.LogError("[FireEffectPool] Prefab has no ParticleSystem!");
      }
      
      return ps;
    }

    private async void ReturnToPoolDelayed(ParticleSystem effect) {
      if (effect == null) return;
      
      // Wait for particles to fade (use main module lifetime)
      var main = effect.main;
      var lifetime = main.startLifetime.constantMax;
      var delayMs = Mathf.Max(500, (int)(lifetime * 1000));
      
      await System.Threading.Tasks.Task.Delay(delayMs);
      
      if (effect == null) return;
      
      effect.gameObject.SetActive(false);
      _pool.Push(effect);
    }
  }
}
