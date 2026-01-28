using System;
using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World.Generation.Pipeline {
  /// <summary>
  /// Shared context passed between generation phases.
  /// Contains input configuration, terrain reference, and generated data.
  /// </summary>
  public class GenerationContext {
    
    // ═══════════════════════════════════════════════════════════════
    // INPUT
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Generation configuration asset</summary>
    public WorldGeneratorConfigSO ConfigSO { get; }
    
    /// <summary>Quick access to config data</summary>
    public WorldGeneratorConfig Config => ConfigSO.Data;
    
    /// <summary>Target terrain to modify</summary>
    public Terrain Terrain { get; }
    
    /// <summary>Random seed for deterministic generation</summary>
    public int Seed { get; }
    
    /// <summary>World bounds for generation</summary>
    public Bounds Bounds { get; }
    
    /// <summary>Artist mode - pause between phases for tweaking</summary>
    public bool IsArtistMode { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // GENERATED DATA (passed between phases)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Biome regions from Phase 1</summary>
    public BiomeMap BiomeMap { get; set; }
    
    /// <summary>Terrain heightmap from Phase 2</summary>
    public float[,] Heightmap { get; set; }
    
    /// <summary>Original heightmap for rollback</summary>
    public float[,] OriginalHeightmap { get; set; }
    
    /// <summary>Terrain splatmap from Phase 3</summary>
    public float[,,] Splatmap { get; set; }
    
    /// <summary>Original splatmap for rollback</summary>
    public float[,,] OriginalSplatmap { get; set; }
    
    /// <summary>River segment data for visualization</summary>
    public float[,] RiverMask { get; set; }
    
    /// <summary>Vegetation detail layers from Phase 4</summary>
    public int[][,] DetailLayers { get; set; }
    
    /// <summary>Original detail layers for rollback</summary>
    public int[][,] OriginalDetailLayers { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // RANDOM
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Deterministic random for consistent generation</summary>
    public System.Random Random { get; }

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════

    public GenerationContext(WorldGeneratorConfigSO configSO, Terrain terrain) {
      ConfigSO = configSO ?? throw new ArgumentNullException(nameof(configSO));
      Terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
      
      // Resolve seed
      Seed = Config.seed != 0 ? Config.seed : Environment.TickCount;
      Random = new System.Random(Seed);
      
      // Calculate bounds
      Bounds = configSO.GetTerrainBounds(terrain);
      
      // Backup original terrain data for rollback
      BackupTerrainData();
    }

    // ═══════════════════════════════════════════════════════════════
    // TERRAIN DATA MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    private void BackupTerrainData() {
      var td = Terrain.terrainData;
      
      // Backup heightmap
      var hRes = td.heightmapResolution;
      OriginalHeightmap = td.GetHeights(0, 0, hRes, hRes);
      
      // Backup splatmap
      OriginalSplatmap = td.GetAlphamaps(0, 0, td.alphamapResolution, td.alphamapResolution);
      
      // Backup detail layers
      var dRes = td.detailResolution;
      var detailCount = td.detailPrototypes.Length;
      OriginalDetailLayers = new int[detailCount][,];
      for (int i = 0; i < detailCount; i++) {
        OriginalDetailLayers[i] = td.GetDetailLayer(0, 0, dRes, dRes, i);
      }
    }

    /// <summary>
    /// Restore terrain to original state (full rollback).
    /// </summary>
    public void RestoreTerrainData() {
      var td = Terrain.terrainData;
      
      if (OriginalHeightmap != null) {
        td.SetHeights(0, 0, OriginalHeightmap);
      }
      
      if (OriginalSplatmap != null) {
        td.SetAlphamaps(0, 0, OriginalSplatmap);
      }
      
      if (OriginalDetailLayers != null) {
        for (int i = 0; i < OriginalDetailLayers.Length; i++) {
          if (OriginalDetailLayers[i] != null) {
            td.SetDetailLayer(0, 0, i, OriginalDetailLayers[i]);
          }
        }
      }
    }

    /// <summary>
    /// Get random value in range [0, 1).
    /// </summary>
    public float RandomValue() => (float)Random.NextDouble();
    
    /// <summary>
    /// Get random value in range [min, max).
    /// </summary>
    public float RandomRange(float min, float max) => min + (float)Random.NextDouble() * (max - min);
    
    /// <summary>
    /// Get random integer in range [min, max).
    /// </summary>
    public int RandomRange(int min, int max) => Random.Next(min, max);
  }
}
