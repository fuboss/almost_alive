using System;
using UnityEngine;

namespace Content.Scripts.World.Generation {
  /// <summary>
  /// Pre-generated spawn data for a single actor.
  /// Used by both runtime generation and preload strategies.
  /// </summary>
  [Serializable]
  public struct WorldSpawnData {
    public string actorKey;
    public Vector3 position;
    public float rotation;
    public float scale;
    public string biomeId;
  }
}

