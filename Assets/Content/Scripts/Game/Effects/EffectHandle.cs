using UnityEngine;

namespace Content.Scripts.Game.Effects {
  public struct EffectHandle {
    public int id;
    public GameObject instance;
    public float startTime;

    public bool isValid => instance != null;

    public static EffectHandle Invalid => new() { id = -1, instance = null };
  }
}
