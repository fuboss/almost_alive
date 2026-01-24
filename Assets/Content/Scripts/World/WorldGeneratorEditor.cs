#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Content.Scripts.World;
using Content.Scripts.World.Generation;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.World {
  /// <summary>
  /// Editor facade for world generation.
  /// Provides menu items and manages incremental actor spawning.
  /// </summary>
  [InitializeOnLoad]
  public static class WorldGeneratorEditor {
    private const string CONTAINER_NAME = "[EditorWorld_Generated]";
    private const int SPAWNS_PER_FRAME = 15;

    private static bool _isGenerating;
    private static SpawningState _state;
    private static EditorActorSpawner _spawner;

    private class SpawningState {
      public EditorGenerationContext Context;
      public int SpawnIndex;
    }

    static WorldGeneratorEditor() {
      EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state) {
      if (state == PlayModeStateChange.ExitingEditMode) {
        var editorContainer = GameObject.Find(CONTAINER_NAME);
        if (editorContainer != null) {
          Debug.Log("[WorldGenEditor] Removing editor-generated world before Play Mode");
          UnityEngine.Object.DestroyImmediate(editorContainer);
        }

        if (_isGenerating) {
          CancelGeneration();
        }

        Cleanup();
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
      CancelGeneration();
      Clear();
    }

    [MenuItem("World/Cancel Generation")]
    public static void CancelGeneration() {
      if (!_isGenerating) return;
      EditorApplication.update -= OnEditorUpdate;
      EditorUtility.ClearProgressBar();
      _isGenerating = false;
      _state = null;
      Cleanup();
      Debug.Log("[WorldGenEditor] Generation cancelled");
    }

    // ═══════════════════════════════════════════════════════════════
    // GENERATION
    // ═══════════════════════════════════════════════════════════════

    public static void Generate(WorldGeneratorConfigSO config, bool saveToPreload = false) {
      if (config == null) {
        Debug.LogError("[WorldGenEditor] No config provided");
        return;
      }

      if (_isGenerating) {
        Debug.LogWarning("[WorldGenEditor] Generation already in progress");
        return;
      }

      if (config.biomes == null || config.biomes.Count == 0) {
        Debug.LogError("[WorldGenEditor] No biomes configured");
        return;
      }

      Clear();

      var terrain = config.terrain != null ? config.terrain : Terrain.activeTerrain;
      if (terrain == null) {
        Debug.LogError("[WorldGenEditor] No terrain found");
        return;
      }

      // Init terrain size
      terrain.terrainData.size = new Vector3(config.size, 200, config.size);
      terrain.transform.localPosition = new Vector3(-config.size / 2f, 0, -config.size / 2f);

      // Create container
      var container = new GameObject(CONTAINER_NAME).transform;
      Undo.RegisterCreatedObjectUndo(container.gameObject, "Generate World");

      // Create context
      var seed = config.seed != 0 ? config.seed : Environment.TickCount;
      var context = new EditorGenerationContext(config, terrain, seed, saveToPreload);

      // Run synchronous generation (biomes, terrain, positions)
      EditorWorldGenerator.Generate(context);

      if (context.BiomeMap == null) {
        EditorUtility.ClearProgressBar();
        return;
      }

      Debug.Log($"[WorldGenEditor] Positions generated: {context.SpawnDataList.Count}");

      // Setup spawning state
      _spawner = new EditorActorSpawner(container);
      _state = new SpawningState {
        Context = context,
        SpawnIndex = 0
      };

      _isGenerating = true;
      EditorApplication.update += OnEditorUpdate;

      Debug.Log($"[WorldGenEditor] Started spawning (seed: {seed}, biomes: {context.BiomeMap.cells.Count})");
    }

    // ═══════════════════════════════════════════════════════════════
    // SPAWNING (incremental per frame)
    // ═══════════════════════════════════════════════════════════════

    private static void OnEditorUpdate() {
      if (!_isGenerating || _state == null) {
        EditorApplication.update -= OnEditorUpdate;
        EditorUtility.ClearProgressBar();
        return;
      }

      var spawnDataList = _state.Context.SpawnDataList;
      var spawnsThisFrame = 0;

      while (_state.SpawnIndex < spawnDataList.Count && spawnsThisFrame < SPAWNS_PER_FRAME) {
        var data = spawnDataList[_state.SpawnIndex];
        _spawner.SpawnActor(data);
        _state.SpawnIndex++;
        spawnsThisFrame++;
      }

      var progress = spawnDataList.Count > 0
        ? (float)_state.SpawnIndex / spawnDataList.Count
        : 1f;

      if (EditorUtility.DisplayCancelableProgressBar("Generating World",
            $"Spawning: {_state.SpawnIndex}/{spawnDataList.Count}",
            0.6f + progress * 0.4f)) {
        CancelGeneration();
        return;
      }

      if (_state.SpawnIndex >= spawnDataList.Count) {
        FinishGeneration();
      }
    }

    private static void FinishGeneration() {
      EditorApplication.update -= OnEditorUpdate;
      EditorUtility.ClearProgressBar();

      var context = _state.Context;
      Debug.Log($"[WorldGenEditor] ✓ Generated {_spawner.SpawnedCount} actors");

      // Build NavMesh
      var navSurface = context.Terrain.GetComponent<NavMeshSurface>();
      if (navSurface != null) navSurface.BuildNavMesh();

      // Save to DevPreloadWorld if requested
      if (context.SaveToPreload) {
        SaveToDevPreloadWorld(context);
      }

      _isGenerating = false;
      _state = null;
      Cleanup();
      SceneView.RepaintAll();
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private static void SaveToDevPreloadWorld(EditorGenerationContext context) {
      var terrain = context.Terrain;
      var preload = terrain.GetComponent<DevPreloadWorld>();
      if (preload == null) {
        preload = Undo.AddComponent<DevPreloadWorld>(terrain.gameObject);
      }

      Undo.RecordObject(preload, "Save to DevPreloadWorld");

      preload.Clear();
      preload.seed = context.Seed;
      preload.spawnDataList = new List<WorldSpawnData>(context.SpawnDataList);
      preload.isPreloaded = true;

      EditorUtility.SetDirty(preload);
      UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);

      Debug.Log($"[WorldGenEditor] ✓ Saved {preload.spawnDataList.Count} actors to DevPreloadWorld");
    }

    public static void Clear() {
      var existing = GameObject.Find(CONTAINER_NAME);
      if (existing != null) Undo.DestroyObjectImmediate(existing);

      var terrain = Terrain.activeTerrain;
      if (terrain != null) {
        var navSurface = terrain.GetComponent<NavMeshSurface>();
        if (navSurface != null) navSurface.BuildNavMesh();
      }
    }

    private static void Cleanup() {
      _spawner?.Dispose();
      _spawner = null;
    }

    private static WorldGeneratorConfigSO LoadConfig() {
      var config = Resources.Load<WorldGeneratorConfigSO>("Environment/WorldGeneratorConfig");
      if (config == null) {
        Debug.LogError("[WorldGenEditor] Config not found at Resources/Environment/");
      }
      return config;
    }
  }
}
#endif
