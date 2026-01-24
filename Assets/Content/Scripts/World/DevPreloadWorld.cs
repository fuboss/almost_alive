using System.Collections.Generic;
using Content.Scripts.World.Generation;
using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Stores pre-generated world data for fast runtime initialization.
  /// Attach to Terrain GameObject. Generate via World/Generate & Save to DevPreloadWorld.
  /// </summary>
  public class DevPreloadWorld : MonoBehaviour {
    [Header("Generation Info")]
    public int seed;
    public bool isPreloaded;
    
    [Header("Spawn Data")]
    public List<WorldSpawnData> spawnDataList = new();
    
    // TODO: Add BiomeMap serialization for runtime biome queries
    // public SerializedBiomeMap biomeMapData;

    public void Clear() {
      spawnDataList.Clear();
      isPreloaded = false;
      seed = 0;
    }
  }
}