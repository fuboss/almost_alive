using System;
using UnityEngine;

namespace Content.Scripts.Game.Effects {
  public struct EffectRequest {
    public GameObject prefab;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public Transform parent;
    public IEffectLifetimeStrategy lifetimeStrategy;
    public Action<EffectHandle> onComplete;

    public static EffectRequest Create(GameObject prefab, Vector3 position) {
      return new EffectRequest {
        prefab = prefab,
        position = position,
        rotation = Quaternion.identity,
        scale = Vector3.one,
        parent = null,
        lifetimeStrategy = null,
        onComplete = null
      };
    }

    public EffectRequest WithRotation(Quaternion rot) {
      rotation = rot;
      return this;
    }

    public EffectRequest WithScale(Vector3 s) {
      scale = s;
      return this;
    }

    public EffectRequest WithParent(Transform p) {
      parent = p;
      return this;
    }

    public EffectRequest WithDuration(float duration) {
      lifetimeStrategy = new DurationLifetimeStrategy(duration);
      return this;
    }

    public EffectRequest WithParticleComplete() {
      lifetimeStrategy = new ParticleCompleteLifetimeStrategy();
      return this;
    }

    public EffectRequest WithLifetime(IEffectLifetimeStrategy strategy) {
      lifetimeStrategy = strategy;
      return this;
    }

    public EffectRequest OnComplete(Action<EffectHandle> callback) {
      onComplete = callback;
      return this;
    }
  }
}
