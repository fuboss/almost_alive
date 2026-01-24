#if UNITY_EDITOR
using System.Collections.Generic;
using Content.Scripts.World.Biomes;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.World.Generation {
  /// <summary>
  /// Context for editor-mode world generation.
  /// Handles Undo registration and progress bar display.
  /// </summary>
  public class EditorGenerationContext {
    public WorldGeneratorConfigSO Config { get; }
    public Terrain Terrain { get; }
    public int Seed { get; }
    public bool SaveToPreload { get; }
    
    // Generation results
    public BiomeMap BiomeMap { get; set; }
    public TerrainFeatureMap FeatureMap { get; set; }
    public List<WorldSpawnData> SpawnDataList { get; } = new();

    public EditorGenerationContext(WorldGeneratorConfigSO config, Terrain terrain, int seed, bool saveToPreload) {
      Config = config;
      Terrain = terrain;
      Seed = seed;
      SaveToPreload = saveToPreload;
    }

    public Bounds GetTerrainBounds() => Config.GetTerrainBounds(Terrain);

    public void UpdateProgress(string status, float progress) {
      EditorUtility.DisplayProgressBar("Generating World", status, progress);
    }

    public void RegisterTerrainUndo(string name) {
      Undo.RegisterCompleteObjectUndo(Terrain.terrainData, name);
    }
  }
}
#endif

