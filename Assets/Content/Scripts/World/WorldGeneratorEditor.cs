#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Content.Scripts.Game;
using Content.Scripts.World;
using Content.Scripts.World.Biomes;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Content.Scripts.Editor.World {
  /// <summary>
  /// Edit-mode world generation with full biome pipeline.
  /// </summary>
  public static class WorldGeneratorEditor {
    private const string CONTAINER_NAME = "[EditorWorld_Generated]";
    private const float RAYCAST_HEIGHT = 50f;
    private const float RAYCAST_DISTANCE = 300f;
    private const int SPAWNS_PER_FRAME = 15;

    private static readonly int GROUND_MASK = LayerMask.GetMask("Default");
    private static readonly Dictionary<string, GameObject> _prefabCache = new();
    private static Transform _container;

    private static bool _isGenerating;
    private static GenerationState _state;

    private class GenerationState {
      public WorldGeneratorConfigSO config;
      public Terrain terrain;
      public BiomeMap biomeMap;
      public TerrainFeatureMap featureMap;
      public List<Vector3> allSpawned = new();
      public Queue<SpawnTask> spawnQueue = new();
      public int totalTarget;
      public int totalSpawned;
      public int biomeIndex;
      public int configIndex;
      public string currentStatus;
    }

    private struct SpawnTask {
      public ScatterRuleSO rule;
      public BiomeScatterConfig config;
      public GameObject prefab;
      public Vector3 position;
      public BiomeType biome;
      public List<ChildSpawnTask> children;
    }

    private struct ChildSpawnTask {
      public ScatterRuleSO rule;
      public GameObject prefab;
      public Vector3 position;
      public int depth;
    }

    [MenuItem("World/Generate (Edit Mode)")]
    public static void GenerateFromMenu() {
      var config = LoadConfig();
      if (config == null) return;
      Generate(config);
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
      Debug.Log("[WorldGenEditor] Generation cancelled");
    }

    public static void Generate(WorldGeneratorConfigSO config) {
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
      _prefabCache.Clear();

      var terrain = config.terrain != null ? config.terrain : Terrain.activeTerrain;
      if (terrain == null) {
        Debug.LogError("[WorldGenEditor] No terrain found");
        return;
      }

      InitTerrain(config, terrain);

      _container = new GameObject(CONTAINER_NAME).transform;
      Undo.RegisterCreatedObjectUndo(_container.gameObject, "Generate World");

      var seed = config.seed != 0 ? config.seed : Environment.TickCount;
      UnityEngine.Random.InitState(seed);

      // Phase 1: Generate Biomes
      EditorUtility.DisplayProgressBar("Generating World", "Creating biome map...", 0.05f);

      var bounds = config.GetTerrainBounds(terrain);
      var biomeMap = VoronoiGenerator.Generate(
        bounds, config.biomes, config.biomeBorderBlend, seed,
        config.minBiomeCells, config.maxBiomeCells
      );

      if (biomeMap == null) {
        EditorUtility.ClearProgressBar();
        Debug.LogError("[WorldGenEditor] Failed to generate biome map");
        return;
      }

      config.cachedBiomeMap = biomeMap;

      // Phase 2: Sculpt Terrain
      if (config.sculptTerrain) {
        EditorUtility.DisplayProgressBar("Generating World", "Sculpting terrain...", 0.15f);
        Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Sculpt Terrain");
        TerrainSculptor.Sculpt(terrain, biomeMap, seed);
      }

      // Phase 3: Paint Splatmap
      if (config.paintSplatmap) {
        EditorUtility.DisplayProgressBar("Generating World", "Painting terrain...", 0.25f);
        Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Paint Splatmap");
        SplatmapPainter.Paint(terrain, biomeMap, seed);
      }

      // Phase 3.5: Generate Feature Map
      EditorUtility.DisplayProgressBar("Generating World", "Analyzing terrain features...", 0.28f);
      var featureMap = TerrainFeatureMap.Generate(terrain);
      config.cachedFeatureMap = featureMap;

      // Phase 4: Spawn Scatters
      var totalTarget = 0;
      if (config.createScattersInEditor) {
        foreach (var biome in config.biomes) {
          if (biome?.scatterConfigs == null) continue;
          foreach (var sc in biome.scatterConfigs) {
            if (sc?.rule == null) continue;
            totalTarget += CalculateTargetCount(sc.rule, bounds);
          }
        }
      }

      _state = new GenerationState {
        config = config,
        terrain = terrain,
        biomeMap = biomeMap,
        featureMap = featureMap,
        totalTarget = totalTarget,
        biomeIndex = 0,
        configIndex = 0,
        currentStatus = "Spawning..."
      };

      _isGenerating = true;
      EditorApplication.update += OnEditorUpdate;

      Debug.Log($"[WorldGenEditor] Started (seed: {seed}, biomes: {biomeMap.cells.Count}, target: {totalTarget})");
    }

    private static void InitTerrain(WorldGeneratorConfigSO config, Terrain terrain) {
      terrain.terrainData.size = new Vector3(config.size, 200, config.size);
      terrain.transform.localPosition = new Vector3(-config.size / 2f, 0, -config.size / 2f);
    }

    public static void Clear() {
      var existing = GameObject.Find(CONTAINER_NAME);
      if (existing != null) Undo.DestroyObjectImmediate(existing);

      var terrain = Terrain.activeTerrain;
      if (terrain != null) {
        var navSurface = terrain.GetComponent<NavMeshSurface>();
        if (navSurface != null) navSurface.BuildNavMesh();
      }
      _container = null;
    }

    private static void OnEditorUpdate() {
      if (!_isGenerating || _state == null) {
        EditorApplication.update -= OnEditorUpdate;
        EditorUtility.ClearProgressBar();
        return;
      }

      var spawnsThisFrame = 0;
      while (_state.spawnQueue.Count > 0 && spawnsThisFrame < SPAWNS_PER_FRAME) {
        var task = _state.spawnQueue.Dequeue();
        if (SpawnActor(task.prefab, task.rule, task.position)) {
          _state.allSpawned.Add(task.position);
          _state.totalSpawned++;
          if (task.children != null) {
            foreach (var child in task.children) EnqueueChildTask(child);
          }
        }
        spawnsThisFrame++;
      }

      if (_state.spawnQueue.Count == 0) LoadNextScatterConfig();

      var progress = _state.totalTarget > 0 ? (float)_state.totalSpawned / _state.totalTarget : 1f;
      if (EditorUtility.DisplayCancelableProgressBar("Generating World",
            $"{_state.currentStatus}: {_state.totalSpawned}/{_state.totalTarget}",
            0.3f + progress * 0.7f)) {
        CancelGeneration();
        return;
      }

      if (_state.spawnQueue.Count == 0 && _state.biomeIndex >= _state.config.biomes.Count) {
        FinishGeneration();
      }
    }

    private static void LoadNextScatterConfig() {
      var config = _state.config;

      while (_state.biomeIndex < config.biomes.Count) {
        var biome = config.biomes[_state.biomeIndex];

        if (biome?.scatterConfigs != null && config.createScattersInEditor) {
          while (_state.configIndex < biome.scatterConfigs.Count) {
            var sc = biome.scatterConfigs[_state.configIndex];
            _state.configIndex++;

            if (sc?.rule == null) continue;

            _state.currentStatus = $"{biome.type}/{sc.rule.actorName}";
            EnqueueScatterTasks(sc, biome.type);

            if (config.logGeneration) {
              Debug.Log($"[WorldGenEditor] Processing: {biome.type} → {sc.rule.actorName} ({sc.placement})");
            }
            return;
          }
        }

        _state.biomeIndex++;
        _state.configIndex = 0;
      }
    }

    private static void EnqueueScatterTasks(BiomeScatterConfig sc, BiomeType biomeType) {
      var rule = sc.rule;
      var prefab = LoadPrefab(rule);
      if (prefab == null) {
        Debug.LogWarning($"[WorldGenEditor] Prefab for '{rule.name}' not found");
        return;
      }

      var bounds = _state.config.GetTerrainBounds(_state.terrain);
      var targetCount = CalculateTargetCount(rule, bounds);

      // Use targeted placement for feature-based scatters
      if (sc.requiresFeatureMap && _state.featureMap != null) {
        var validPositions = _state.featureMap.GetValidPositions(sc.placement);
        Debug.Log($"[WorldGenEditor] {sc.placement}: {validPositions.Count} valid positions found");
        EnqueueTargetedTasks(sc, prefab, biomeType, validPositions, targetCount);
        return;
      }

      if (rule.useClustering) {
        EnqueueClusteredTasks(sc, prefab, biomeType, bounds, targetCount);
      } else {
        EnqueueUniformTasks(sc, prefab, biomeType, bounds, targetCount);
      }
    }

    private static void EnqueueTargetedTasks(BiomeScatterConfig sc, GameObject prefab, BiomeType biomeType,
      List<Vector3> validPositions, int targetCount) {
      if (validPositions.Count == 0) return;
      
      var rule = sc.rule;
      var placed = 0;
      
      // Shuffle positions for randomness
      for (var i = validPositions.Count - 1; i > 0; i--) {
        var j = UnityEngine.Random.Range(0, i + 1);
        (validPositions[i], validPositions[j]) = (validPositions[j], validPositions[i]);
      }
      
      foreach (var pos in validPositions) {
        if (placed >= targetCount) break;
        
        // Check biome
        if (_state.biomeMap.GetBiomeAt(pos) != biomeType) continue;
        
        // Skip slope/height validation - feature map already validated position
        // Only check spacing
        if (!ValidateSpacing(rule.minSpacing, pos, _state.allSpawned)) continue;
        
        _state.spawnQueue.Enqueue(new SpawnTask {
          rule = rule,
          config = sc,
          prefab = prefab,
          position = pos,
          biome = biomeType,
          children = rule.hasChildren ? PrepareChildTasks(rule, pos) : null
        });
        
        _state.allSpawned.Add(pos);
        placed++;
      }
      
      if (_state.config.logGeneration) {
        Debug.Log($"[WorldGenEditor] Targeted placement: {placed}/{targetCount} for {sc.placement}");
      }
    }

    private static void EnqueueUniformTasks(BiomeScatterConfig sc, GameObject prefab, BiomeType biomeType,
      Bounds bounds, int targetCount) {
      var rule = sc.rule;
      var placed = 0;
      var attempts = 0;
      var maxAttempts = targetCount * rule.maxAttempts;

      while (placed < targetCount && attempts < maxAttempts) {
        attempts++;
        var pos = RandomPointInBounds(bounds);

        if (_state.biomeMap.GetBiomeAt(pos) != biomeType) continue;
        if (!ValidatePlacement(sc, pos, _state.allSpawned)) continue;

        _state.spawnQueue.Enqueue(new SpawnTask {
          rule = rule,
          config = sc,
          prefab = prefab,
          position = pos,
          biome = biomeType,
          children = rule.hasChildren ? PrepareChildTasks(rule, pos) : null
        });

        _state.allSpawned.Add(pos);
        placed++;
      }
    }

    private static void EnqueueClusteredTasks(BiomeScatterConfig sc, GameObject prefab, BiomeType biomeType,
      Bounds bounds, int targetCount) {
      var rule = sc.rule;
      var remaining = targetCount;
      var clusterAttempts = 0;
      var maxClusterAttempts = targetCount * 10;

      while (remaining > 0 && clusterAttempts < maxClusterAttempts) {
        clusterAttempts++;
        var clusterCenter = RandomPointInBounds(bounds);

        if (_state.biomeMap.GetBiomeAt(clusterCenter) != biomeType) continue;
        if (!ValidateTerrainAt(sc, clusterCenter)) continue;

        var clusterCount = Mathf.Min(
          UnityEngine.Random.Range(rule.clusterSize.x, rule.clusterSize.y + 1),
          remaining
        );

        for (var i = 0; i < clusterCount; i++) {
          var offset = UnityEngine.Random.insideUnitCircle * rule.clusterSpread;
          var pos = clusterCenter + new Vector3(offset.x, 0, offset.y);

          if (_state.biomeMap.GetBiomeAt(pos) != biomeType) continue;
          if (!ValidatePlacement(sc, pos, _state.allSpawned)) continue;

          _state.spawnQueue.Enqueue(new SpawnTask {
            rule = rule,
            config = sc,
            prefab = prefab,
            position = pos,
            biome = biomeType,
            children = rule.hasChildren ? PrepareChildTasks(rule, pos) : null
          });

          _state.allSpawned.Add(pos);
          remaining--;
        }
      }
    }

    private static List<ChildSpawnTask> PrepareChildTasks(ScatterRuleSO parentRule, Vector3 parentPos, int depth = 0) {
      const int MAX_DEPTH = 3;
      if (depth >= MAX_DEPTH || parentRule.childScatters == null) return null;

      var result = new List<ChildSpawnTask>();

      foreach (var childConfig in parentRule.childScatters) {
        if (childConfig?.rule == null) continue;

        var childRule = childConfig.rule;
        var childPrefab = LoadPrefab(childRule);
        if (childPrefab == null) continue;

        var count = UnityEngine.Random.Range(childConfig.countPerParent.x, childConfig.countPerParent.y + 1);
        var localSpawned = new List<Vector3>();

        for (var i = 0; i < count; i++) {
          var attempts = 0;
          while (attempts < childRule.maxAttempts) {
            attempts++;

            var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            var radius = UnityEngine.Random.Range(childConfig.radiusMin, childConfig.radiusMax);
            var pos = parentPos + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            if (!ValidateTerrainAtRule(childRule, pos)) continue;
            if (childConfig.inheritTerrainFilter && !ValidateTerrainAtRule(parentRule, pos)) continue;

            var spacing = childConfig.localSpacingOnly
              ? ValidateLocalSpacing(childRule.minSpacing, pos, localSpawned)
              : ValidateSpacing(childRule.minSpacing, pos, _state.allSpawned);

            if (!spacing) continue;

            result.Add(new ChildSpawnTask {
              rule = childRule,
              prefab = childPrefab,
              position = pos,
              depth = depth + 1
            });

            localSpawned.Add(pos);
            _state.allSpawned.Add(pos);
            break;
          }
        }
      }

      return result.Count > 0 ? result : null;
    }

    private static void EnqueueChildTask(ChildSpawnTask child) {
      _state.spawnQueue.Enqueue(new SpawnTask {
        rule = child.rule,
        prefab = child.prefab,
        position = child.position,
        children = child.rule.hasChildren ? PrepareChildTasks(child.rule, child.position, child.depth) : null
      });
    }

    private static void FinishGeneration() {
      EditorApplication.update -= OnEditorUpdate;
      EditorUtility.ClearProgressBar();

      Debug.Log($"[WorldGenEditor] ✓ Generated {_state.totalSpawned} actors");

      var terrain = Terrain.activeTerrain;
      if (terrain != null) {
        var navSurface = terrain.GetComponent<NavMeshSurface>();
        if (navSurface != null) navSurface.BuildNavMesh();
      }

      _isGenerating = false;
      _state = null;
      _prefabCache.Clear();
      SceneView.RepaintAll();
    }

    private static bool SpawnActor(GameObject prefab, ScatterRuleSO rule, Vector3 position) {
      if (_container == null) return false;

      var groundedPos = CalculateGroundedPosition(position);
      var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _container);
      if (instance == null) return false;

      instance.transform.position = groundedPos;

      if (rule.randomRotation) {
        instance.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
      }

      var scale = UnityEngine.Random.Range(rule.scaleRange.x, rule.scaleRange.y);
      instance.transform.localScale = Vector3.one * scale;

      Undo.RegisterCreatedObjectUndo(instance, "Spawn Actor");
      return true;
    }

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════════

    private static bool ValidatePlacement(BiomeScatterConfig sc, Vector3 pos, List<Vector3> spawned) {
      if (!ValidateTerrainAt(sc, pos)) return false;
      if (!ValidateSpacing(sc.rule.minSpacing, pos, spawned)) return false;
      
      // Check feature map for edge-aware placements
      if (sc.requiresFeatureMap && _state.featureMap != null) {
        if (!_state.featureMap.CheckPlacement(pos, sc.placement)) return false;
      }
      
      return true;
    }

    private static bool ValidateTerrainAt(BiomeScatterConfig sc, Vector3 worldPos) {
      var terrain = _state.terrain;
      var terrainPos = terrain.transform.position;
      var terrainData = terrain.terrainData;
      var size = terrainData.size;

      var normalizedX = (worldPos.x - terrainPos.x) / size.x;
      var normalizedZ = (worldPos.z - terrainPos.z) / size.z;

      if (normalizedX < 0 || normalizedX > 1 || normalizedZ < 0 || normalizedZ > 1)
        return false;

      var height = terrain.SampleHeight(worldPos);
      var heightRange = sc.GetHeightRange();
      if (height < heightRange.x || height > heightRange.y)
        return false;

      var slope = terrainData.GetSteepness(normalizedX, normalizedZ);
      var slopeRange = sc.GetPlacementSlopeRange();
      if (slope < slopeRange.x || slope > slopeRange.y)
        return false;

      var rule = sc.rule;
      if (rule.allowedTerrainLayers is { Length: > 0 }) {
        var alphamapX = Mathf.RoundToInt(normalizedX * (terrainData.alphamapWidth - 1));
        var alphamapZ = Mathf.RoundToInt(normalizedZ * (terrainData.alphamapHeight - 1));
        var alphas = terrainData.GetAlphamaps(alphamapX, alphamapZ, 1, 1);

        var maxAlpha = 0f;
        var dominantLayer = 0;
        for (var i = 0; i < alphas.GetLength(2); i++) {
          if (alphas[0, 0, i] > maxAlpha) {
            maxAlpha = alphas[0, 0, i];
            dominantLayer = i;
          }
        }

        if (Array.IndexOf(rule.allowedTerrainLayers, dominantLayer) < 0)
          return false;
      }

      return true;
    }

    private static bool ValidateTerrainAtRule(ScatterRuleSO rule, Vector3 worldPos) {
      var terrain = _state.terrain;
      var terrainPos = terrain.transform.position;
      var terrainData = terrain.terrainData;
      var size = terrainData.size;

      var normalizedX = (worldPos.x - terrainPos.x) / size.x;
      var normalizedZ = (worldPos.z - terrainPos.z) / size.z;

      if (normalizedX < 0 || normalizedX > 1 || normalizedZ < 0 || normalizedZ > 1)
        return false;

      var height = terrain.SampleHeight(worldPos);
      if (height < rule.heightRange.x || height > rule.heightRange.y)
        return false;

      var slope = terrainData.GetSteepness(normalizedX, normalizedZ);
      if (slope < rule.slopeRange.x || slope > rule.slopeRange.y)
        return false;

      return true;
    }

    private static bool ValidateSpacing(float minSpacing, Vector3 position, List<Vector3> spawned) {
      var sqrSpacing = minSpacing * minSpacing;
      foreach (var pos in spawned) {
        if ((pos - position).sqrMagnitude < sqrSpacing) return false;
      }
      return true;
    }

    private static bool ValidateLocalSpacing(float minSpacing, Vector3 position, List<Vector3> siblings) {
      var sqrSpacing = minSpacing * minSpacing;
      foreach (var pos in siblings) {
        if ((pos - position).sqrMagnitude < sqrSpacing) return false;
      }
      return true;
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private static WorldGeneratorConfigSO LoadConfig() {
      var config = Resources.Load<WorldGeneratorConfigSO>("Environment/WorldGeneratorConfig");
      if (config == null) {
        Debug.LogError("[WorldGenEditor] Config not found at Resources/Environment/");
      }
      return config;
    }

    private static GameObject LoadPrefab(ScatterRuleSO rule) {
      if (string.IsNullOrWhiteSpace(rule.actorKey) && rule.prefab != null) {
        return rule.prefab;
      }

      if (_prefabCache.TryGetValue(rule.actorKey, out var cached)) return cached;

      var handle = Addressables.LoadAssetsAsync<GameObject>("Actors", null);
      var prefabs = handle.WaitForCompletion();

      foreach (var prefab in prefabs) {
        var actor = prefab.GetComponent<ActorDescription>();
        if (actor != null && actor.actorKey == rule.actorKey) {
          _prefabCache[rule.actorKey] = prefab;
          return prefab;
        }
      }
      return null;
    }

    private static Vector3 CalculateGroundedPosition(Vector3 targetPos) {
      var rayOrigin = new Vector3(targetPos.x, targetPos.y + RAYCAST_HEIGHT, targetPos.z);
      return Physics.Raycast(rayOrigin, Vector3.down, out var hit, RAYCAST_DISTANCE, GROUND_MASK)
        ? hit.point
        : targetPos;
    }

    private static Vector3 RandomPointInBounds(Bounds bounds) {
      return new Vector3(
        UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
        0,
        UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
      );
    }

    private static int CalculateTargetCount(ScatterRuleSO rule, Bounds bounds) {
      if (rule.fixedCount > 0) return rule.fixedCount;
      var area = bounds.size.x * bounds.size.z;
      return Mathf.RoundToInt(area / 100f * rule.density);
    }
  }
}
#endif
