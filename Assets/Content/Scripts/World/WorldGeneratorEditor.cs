#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Content.Scripts.World;
using Content.Scripts.World.Generation;
using Content.Scripts.World.Generation.Pipeline;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.World {
  /// <summary>
  /// Editor facade for world generation.
  /// Uses GenerationPipeline for consistent results with ArtistModeWindow.
  /// </summary>
  [InitializeOnLoad]
  public static class WorldGeneratorEditor {
    private const string SCATTER_CONTAINER = "[Generated_Scatters]";

    private static GenerationPipeline _pipeline;

    static WorldGeneratorEditor() {
      EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state) {
      if (state == PlayModeStateChange.ExitingEditMode) {
        // Cleanup editor-generated objects before Play Mode
        CleanupGeneratedObjects();
        _pipeline = null;
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // MENU ITEMS
    // ═══════════════════════════════════════════════════════════════

    [MenuItem("World/Generate (Edit Mode)")]
    public static void GenerateFromMenu() {
      var config = LoadConfig();
      if (config == null) return;
      Generate(config);
    }

    [MenuItem("World/Generate & Save to DevPreloadWorld")]
    public static void GenerateAndSaveFromMenu() {
      var config = LoadConfig();
      if (config == null) return;
      Generate(config, saveToPreload: true);
    }

    [MenuItem("World/Clear Generated")]
    public static void ClearFromMenu() {
      Clear();
    }

    // ═══════════════════════════════════════════════════════════════
    // GENERATION
    // ═══════════════════════════════════════════════════════════════

    public static void Generate(WorldGeneratorConfigSO config, bool saveToPreload = false) {
      if (config == null) {
        Debug.LogError("[WorldGenEditor] No config provided");
        return;
      }

      if (config.Data.biomes == null || config.Data.biomes.Count == 0) {
        Debug.LogError("[WorldGenEditor] No biomes configured");
        return;
      }

      var terrain = config.terrain != null ? config.terrain : Terrain.activeTerrain;
      if (terrain == null) {
        Debug.LogError("[WorldGenEditor] No terrain found");
        return;
      }

      // Clear previous generation
      Clear();

      // Init terrain size
      terrain.terrainData.size = new Vector3(config.Data.size, 200, config.Data.size);
      terrain.transform.localPosition = new Vector3(-config.Data.size / 2f, 0, -config.Data.size / 2f);

      // Create and run pipeline (non-artist mode = run all phases)
      _pipeline = new GenerationPipeline();
      
      var startTime = DateTime.Now;
      
      // Subscribe to completion
      _pipeline.OnPipelineCompleted += () => {
        var elapsed = (DateTime.Now - startTime).TotalSeconds;
        Debug.Log($"[WorldGenEditor] ✓ Generation completed in {elapsed:F2}s");
        
        // Build NavMesh
        var navSurface = terrain.GetComponent<NavMeshSurface>();
        if (navSurface != null) {
          navSurface.BuildNavMesh();
          Debug.Log("[WorldGenEditor] NavMesh rebuilt");
        }

        // Save to DevPreloadWorld if requested
        if (saveToPreload) {
          SaveToDevPreloadWorld(terrain, _pipeline.Context);
        }
        
        SceneView.RepaintAll();
      };
      
      _pipeline.OnPhaseFailed += phase => {
        Debug.LogError($"[WorldGenEditor] Phase '{phase.Name}' failed!");
        EditorUtility.ClearProgressBar();
      };

      // Run all phases synchronously (artistMode = false)
      _pipeline.Begin(config, terrain, artistMode: false);
    }

    // ═══════════════════════════════════════════════════════════════
    // CLEAR
    // ═══════════════════════════════════════════════════════════════

    public static void Clear() {
      // Reset pipeline if exists
      _pipeline?.Reset();
      _pipeline = null;
      
      // Cleanup generated objects
      CleanupGeneratedObjects();

      var terrain = Terrain.activeTerrain;
      if (terrain != null) {
        // Restore terrain to flat state
        var td = terrain.terrainData;
        
        // Flatten heightmap
        var hRes = td.heightmapResolution;
        var flatHeights = new float[hRes, hRes];
        td.SetHeights(0, 0, flatHeights);

        // Clear splatmap to first layer
        var aRes = td.alphamapResolution;
        var layerCount = td.alphamapLayers;
        var clearSplatmap = new float[aRes, aRes, layerCount];
        for (int y = 0; y < aRes; y++) {
          for (int x = 0; x < aRes; x++) {
            clearSplatmap[y, x, 0] = 1f; // First layer only
          }
        }
        td.SetAlphamaps(0, 0, clearSplatmap);

        // Clear vegetation details
        var dRes = td.detailResolution;
        var emptyLayer = new int[dRes, dRes];
        for (int i = 0; i < td.detailPrototypes.Length; i++) {
          td.SetDetailLayer(0, 0, i, emptyLayer);
        }

        // Rebuild NavMesh
        var navSurface = terrain.GetComponent<NavMeshSurface>();
        if (navSurface != null) {
          navSurface.BuildNavMesh();
        }
        
        Debug.Log("[WorldGenEditor] Terrain cleared");
      }
    }

    private static void CleanupGeneratedObjects() {
      // Destroy scatter container
      var scatterRoot = GameObject.Find(SCATTER_CONTAINER);
      if (scatterRoot != null) {
        UnityEngine.Object.DestroyImmediate(scatterRoot);
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // SAVE TO PRELOAD
    // ═══════════════════════════════════════════════════════════════

    private static void SaveToDevPreloadWorld(Terrain terrain, GenerationContext context) {
      if (context == null) {
        Debug.LogWarning("[WorldGenEditor] No context to save");
        return;
      }
      
      var preload = terrain.GetComponent<DevPreloadWorld>();
      if (preload == null) {
        preload = Undo.AddComponent<DevPreloadWorld>(terrain.gameObject);
      }

      Undo.RecordObject(preload, "Save to DevPreloadWorld");

      preload.Clear();
      preload.seed = context.Seed;
      
      // Collect spawn data from generated scatters
      var spawnDataList = CollectSpawnDataFromScene();
      preload.spawnDataList = spawnDataList;
      preload.isPreloaded = true;

      EditorUtility.SetDirty(preload);
      UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);

      Debug.Log($"[WorldGenEditor] ✓ Saved {spawnDataList.Count} actors to DevPreloadWorld");
    }

    /// <summary>
    /// Collect WorldSpawnData from spawned actors in scene.
    /// </summary>
    private static List<WorldSpawnData> CollectSpawnDataFromScene() {
      var result = new List<WorldSpawnData>();
      
      var scatterRoot = GameObject.Find(SCATTER_CONTAINER);
      if (scatterRoot == null) return result;

      // Iterate through all children (biome containers) and their children (actors)
      foreach (Transform biomeContainer in scatterRoot.transform) {
        var biomeId = biomeContainer.name;
        
        foreach (Transform actor in biomeContainer) {
          var actorDesc = actor.GetComponent<Content.Scripts.Game.ActorDescription>();
          if (actorDesc == null) continue;
          
          result.Add(new WorldSpawnData {
            actorKey = actorDesc.actorKey,
            position = actor.position,
            rotation = actor.eulerAngles.y,
            scale = actor.localScale.x,
            biomeId = biomeId
          });
        }
      }

      return result;
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private static WorldGeneratorConfigSO LoadConfig() {
      var config = Resources.Load<WorldGeneratorConfigSO>("Environment/WorldGeneratorConfig");
      if (config == null) {
        Debug.LogError("[WorldGenEditor] Config not found at Resources/Environment/WorldGeneratorConfig");
      }
      return config;
    }
  }
}
#endif
