using System;
using System.Collections.Generic;
using System.Threading;
using Content.Scripts.AI.GOAP;
using Content.Scripts.Game;
using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World.Generation {
  /// <summary>
  /// Shared context for world generation strategies.
  /// Contains all dependencies and state needed during generation.
  /// </summary>
  public class WorldGenerationContext {
    public WorldGeneratorConfigSO config { get; }
    public Terrain terrain { get; }
    public ActorCreationModule actorCreation { get; }
    public CancellationToken cancellationToken { get; }
    
    // Generation state
    public BiomeMap biomeMap { get; set; }
    public TerrainFeatureMap featureMap { get; set; }
    public List<WorldSpawnData> spawnDataList { get; } = new();
    public int seed { get; set; }
    
    // Progress reporting
    public Action<float> onProgress { get; set; }
    
    public WorldGenerationContext(
      WorldGeneratorConfigSO config,
      Terrain terrain,
      ActorCreationModule actorCreation,
      CancellationToken ct) {
      this.config = config;
      this.terrain = terrain;
      this.actorCreation = actorCreation;
      cancellationToken = ct;
    }

    public Bounds GetTerrainBounds() {
      var pos = terrain.transform.position;
      var size = terrain.terrainData.size;
      return new Bounds(
        pos + size * 0.5f,
        new Vector3(size.x - config.edgeMargin * 2, size.y, size.z - config.edgeMargin * 2)
      );
    }
  }
}

