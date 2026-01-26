using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Game.Effects {
  public class EffectsModule : IInitializable, ITickable, IDisposable {
    private readonly EffectPool _pool;
    private readonly List<ActiveEffect> _activeEffects = new();
    private readonly List<ActiveEffect> _toRemove = new();

    private int _nextId = 1;
    private Transform _effectsRoot;

    private struct ActiveEffect {
      public EffectHandle handle;
      public GameObject prefab;
      public IEffectLifetimeStrategy lifetimeStrategy;
      public Action<EffectHandle> onComplete;
    }

    public EffectsModule() {
      _pool = new EffectPool();
    }

    void IInitializable.Initialize() {
      var effectsGO = new GameObject("[Effects]");
      effectsGO.transform.SetParent(null);
      _effectsRoot = effectsGO.transform;
    }

    void ITickable.Tick() {
      float time = Time.time;

      foreach (var effect in _activeEffects) {
        if (!effect.handle.isValid) {
          _toRemove.Add(effect);
          continue;
        }

        float elapsed = time - effect.handle.startTime;
        if (effect.lifetimeStrategy?.ShouldComplete(effect.handle, elapsed) ?? false) {
          _toRemove.Add(effect);
        }
      }

      foreach (var effect in _toRemove) {
        CompleteEffect(effect);
      }
      _toRemove.Clear();
    }

    public EffectHandle Spawn(EffectRequest request) {
      if (request.prefab == null) return EffectHandle.Invalid;

      var instance = _pool.Get(request.prefab);
      if (instance == null) return EffectHandle.Invalid;

      var t = instance.transform;
      if (request.parent != null) {
        t.SetParent(request.parent);
        t.localPosition = request.position;
        t.localRotation = request.rotation;
      } else {
        t.SetParent(_effectsRoot);
        t.position = request.position;
        t.rotation = request.rotation;
      }
      t.localScale = request.scale;

      var handle = new EffectHandle {
        id = _nextId++,
        instance = instance,
        startTime = Time.time
      };

      request.lifetimeStrategy?.OnStart(handle);

      var ps = instance.GetComponent<ParticleSystem>();
      if (ps != null) {
        ps.Clear();
        ps.Play();
      }

      _activeEffects.Add(new ActiveEffect {
        handle = handle,
        prefab = request.prefab,
        lifetimeStrategy = request.lifetimeStrategy ?? new ParticleCompleteLifetimeStrategy(),
        onComplete = request.onComplete
      });

      return handle;
    }

    public async UniTask<EffectHandle> SpawnAsync(EffectRequest request) {
      var handle = Spawn(request);
      if (!handle.isValid) return handle;

      await UniTask.WaitUntil(() => !IsActive(handle.id));
      return handle;
    }

    public EffectHandle SpawnAt(GameObject prefab, Vector3 position, float duration) {
      return Spawn(EffectRequest.Create(prefab, position).WithDuration(duration));
    }

    public EffectHandle SpawnAttached(GameObject prefab, Transform parent, Vector3 localPos, float duration) {
      return Spawn(EffectRequest.Create(prefab, localPos).WithParent(parent).WithDuration(duration));
    }

    public void Stop(int handleId) {
      for (int i = 0; i < _activeEffects.Count; i++) {
        if (_activeEffects[i].handle.id == handleId) {
          CompleteEffect(_activeEffects[i]);
          _activeEffects.RemoveAt(i);
          return;
        }
      }
    }

    public void StopAll() {
      foreach (var effect in _activeEffects) {
        CompleteEffect(effect);
      }
      _activeEffects.Clear();
    }

    public bool IsActive(int handleId) {
      foreach (var effect in _activeEffects) {
        if (effect.handle.id == handleId) return true;
      }
      return false;
    }

    private void CompleteEffect(ActiveEffect effect) {
      effect.lifetimeStrategy?.OnComplete(effect.handle);
      effect.onComplete?.Invoke(effect.handle);

      if (effect.handle.instance != null) {
        var ps = effect.handle.instance.GetComponent<ParticleSystem>();
        if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        _pool.Return(effect.prefab, effect.handle.instance);
      }

      _activeEffects.Remove(effect);
    }

    void IDisposable.Dispose() {
      StopAll();
      _pool.Clear();
      if (_effectsRoot != null) {
        UnityEngine.Object.Destroy(_effectsRoot.gameObject);
      }
    }
  }
}
