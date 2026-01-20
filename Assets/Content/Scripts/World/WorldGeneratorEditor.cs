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
  /// Two-phase generation for determinism: positions first, then transforms.
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
      public string currentStatus;
      
      public WorldRandom positionRandom;
      public WorldRandom transformRandom;
      
      // Two-phase data
      public List<(string actorKey, Vector3 position, ScatterRuleSO rule)> allPositions = new();
      public List<Vector3> spawnedPositions = new();
      public List<SpawnData> spawnQueue = new();
      public int spawnIndex;
    }

    private struct SpawnData {
      public string actorKey;
      public Vector3 position;
      public float rotation;
      public float scale;
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
      var positionRandom = new WorldRandom(seed);
      var transformRandom = new WorldRandom(seed + 1000);

      Debug.Log($"[EDITOR] ========== GENERATION START ==========");
      Debug.Log($"[EDITOR] Seed: {seed}");

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

      if (config.sculptTerrain) {
        EditorUtility.DisplayProgressBar("Generating World", "Sculpting terrain...", 0.15f);
        Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Sculpt Terrain");
        TerrainSculptor.Sculpt(terrain, biomeMap, seed);
      }

      if (config.paintSplatmap) {
        EditorUtility.DisplayProgressBar("Generating World", "Painting terrain...", 0.25f);
        Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Paint Splatmap");
        SplatmapPainter.Paint(terrain, biomeMap, seed);
      }

      EditorUtility.DisplayProgressBar("Generating World", "Analyzing terrain features...", 0.28f);
      var featureMap = TerrainFeatureMap.Generate(terrain);
      config.cachedFeatureMap = featureMap;

      _state = new GenerationState {
        config = config,
        terrain = terrain,
        biomeMap = biomeMap,
        featureMap = featureMap,
        currentStatus = "Generating positions...",
        positionRandom = positionRandom,
        transformRandom = transformRandom
      };

      // Phase 1: Generate ALL positions (positionRandom only)
      EditorUtility.DisplayProgressBar("Generating World", "Generating positions...", 0.30f);
      
      if (config.createScattersInEditor) {
        foreach (var biome in config.biomes) {
          if (biome?.scatterConfigs == null) continue;

          foreach (var sc in biome.scatterConfigs) {
            if (sc?.rule == null) continue;

            var rule = sc.rule;
            var targetCount = CalculateTargetCount(rule, bounds);

            if (config.logGeneration) {
              Debug.Log($"[EDITOR] Processing: {rule.actorName}, useClustering={rule.useClustering}, clusterSize={rule.clusterSize}");
            }

            if (rule.useClustering) {
              GenerateClusteredPositions(sc, biome.type, bounds, targetCount);
            } else {
              GenerateUniformPositions(sc, biome.type, bounds, targetCount);
            }
          }
        }
      }

      Debug.Log($"[EDITOR] ========== POSITIONS COMPLETE ==========");
      Debug.Log($"[EDITOR] Total positions generated: {_state.allPositions.Count}");

      // Phase 2: Generate transforms for all positions (transformRandom only)
      EditorUtility.DisplayProgressBar("Generating World", "Generating transforms...", 0.50f);
      
      foreach (var (actorKey, position, rule) in _state.allPositions) {
        var rotation = rule.randomRotation ? _state.transformRandom.Range(0f, 360f) : 0f;
        var scale = _state.transformRandom.Range(rule.scaleRange.x, rule.scaleRange.y);
        
        _state.spawnQueue.Add(new SpawnData {
          actorKey = actorKey,
          position = position,
          rotation = rotation,
          scale = scale
        });
      }

      _state.currentStatus = "Spawning...";
      _isGenerating = true;
      EditorApplication.update += OnEditorUpdate;

      Debug.Log($"[WorldGenEditor] Started (seed: {seed}, biomes: {biomeMap.cells.Count}, positions: {_state.allPositions.Count})");
    }

    // ═══════════════════════════════════════════════════════════════
    // POSITION GENERATION (positionRandom only)
    // ═══════════════════════════════════════════════════════════════

    private static void GenerateUniformPositions(BiomeScatterConfig sc, BiomeType biomeType,
      Bounds bounds, int targetCount) {
      var rule = sc.rule;
      var placed = 0;
      var attempts = 0;
      var maxAttempts = targetCount * rule.maxAttempts;

      Debug.Log($"[EDITOR] GenerateUniformPositions: {rule.actorName}, target={targetCount}");

      while (placed < targetCount && attempts < maxAttempts) {
        attempts++;

        var pos = _state.positionRandom.RandomPointInBounds(bounds);
        if (_state.biomeMap.GetBiomeAt(pos) != biomeType) continue;
        if (!ValidatePlacement(sc, pos)) continue;

        _state.allPositions.Add((rule.actorKey, pos, rule));
        _state.spawnedPositions.Add(pos);
        placed++;

        // Log first 3 positions for comparison
        if (placed <= 3) {
          Debug.Log($"[EDITOR] Uniform pos #{placed}: {pos:F2}");
        }

        if (rule.hasChildren) {
          GenerateChildPositions(rule, pos);
        }
      }
    }

    private static void GenerateClusteredPositions(BiomeScatterConfig sc, BiomeType biomeType,
      Bounds bounds, int targetCount) {
      var rule = sc.rule;
      var remaining = targetCount;
      var clusterAttempts = 0;
      var maxClusterAttempts = targetCount * 10;
      var totalPlaced = 0;

      Debug.Log($"[EDITOR] GenerateClusteredPositions: {rule.actorName}, target={targetCount}, clusterSize={rule.clusterSize}, spread={rule.clusterSpread}");

      while (remaining > 0 && clusterAttempts < maxClusterAttempts) {
        clusterAttempts++;

        var clusterCenter = _state.positionRandom.RandomPointInBounds(bounds);
        if (_state.biomeMap.GetBiomeAt(clusterCenter) != biomeType) continue;
        if (!ValidateTerrainAt(sc, clusterCenter)) continue;

        var clusterCount = Mathf.Min(
          _state.positionRandom.Range(rule.clusterSize.x, rule.clusterSize.y + 1),
          remaining
        );

        // Track positions within THIS cluster only
        var clusterLocalPositions = new List<Vector3>();
        var clusterPlaced = 0;
        var clusterStartIndex = _state.spawnedPositions.Count; // Index before this cluster

        for (var i = 0; i < clusterCount; i++) {
          var offset = _state.positionRandom.InsideUnitCircle() * rule.clusterSpread;
          var pos = clusterCenter + new Vector3(offset.x, 0, offset.y);

          if (_state.biomeMap.GetBiomeAt(pos) != biomeType) continue;
          if (!ValidateTerrainAt(sc, pos)) continue;
          
          // Check spacing only against positions BEFORE this cluster started
          if (!ValidateSpacingRange(rule.minSpacing, pos, _state.spawnedPositions, 0, clusterStartIndex)) continue;
          
          // Inside cluster: use reduced spacing (30% of minSpacing) between cluster members
          if (!ValidateLocalSpacing(rule.minSpacing * 0.3f, pos, clusterLocalPositions)) continue;

          _state.allPositions.Add((rule.actorKey, pos, rule));
          _state.spawnedPositions.Add(pos);
          clusterLocalPositions.Add(pos);
          remaining--;
          totalPlaced++;
          clusterPlaced++;

          if (totalPlaced <= 3) {
            Debug.Log($"[EDITOR] Clustered pos #{totalPlaced}: {pos:F2} (cluster center: {clusterCenter:F2})");
          }

          if (rule.hasChildren) {
            GenerateChildPositions(rule, pos);
          }
        }
        
        if (clusterPlaced > 0) {
          Debug.Log($"[EDITOR] Cluster placed {clusterPlaced}/{clusterCount} at center {clusterCenter:F2}");
        }
      }
    }

    private static void GenerateChildPositions(ScatterRuleSO parentRule, Vector3 parentPos, int depth = 0) {
      const int MAX_DEPTH = 3;
      if (depth >= MAX_DEPTH || parentRule.childScatters == null) return;

      foreach (var childConfig in parentRule.childScatters) {
        if (childConfig?.rule == null) continue;

        var childRule = childConfig.rule;
        var count = _state.positionRandom.Range(childConfig.countPerParent.x, childConfig.countPerParent.y + 1);
        var localSpawned = new List<Vector3>();

        for (var i = 0; i < count; i++) {
          var attempts = 0;
          while (attempts < childRule.maxAttempts) {
            attempts++;

            var angle = _state.positionRandom.Range(0f, Mathf.PI * 2f);
            var radius = _state.positionRandom.Range(childConfig.radiusMin, childConfig.radiusMax);
            var pos = parentPos + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            if (!ValidateTerrainAtRule(childRule, pos)) continue;
            if (childConfig.inheritTerrainFilter && !ValidateTerrainAtRule(parentRule, pos)) continue;

            var spacing = childConfig.localSpacingOnly
              ? ValidateLocalSpacing(childRule.minSpacing, pos, localSpawned)
              : ValidateSpacing(childRule.minSpacing, pos, _state.spawnedPositions);

            if (!spacing) continue;

            _state.allPositions.Add((childRule.actorKey, pos, childRule));
            localSpawned.Add(pos);
            _state.spawnedPositions.Add(pos);

            if (childRule.hasChildren) {
              GenerateChildPositions(childRule, pos, depth + 1);
            }
            break;
          }
        }
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // SPAWNING
    // ═══════════════════════════════════════════════════════════════

    private static void OnEditorUpdate() {
      if (!_isGenerating || _state == null) {
        EditorApplication.update -= OnEditorUpdate;
        EditorUtility.ClearProgressBar();
        return;
      }

      var spawnsThisFrame = 0;
      while (_state.spawnIndex < _state.spawnQueue.Count && spawnsThisFrame < SPAWNS_PER_FRAME) {
        var data = _state.spawnQueue[_state.spawnIndex];
        SpawnActor(data);
        _state.spawnIndex++;
        spawnsThisFrame++;
      }

      var progress = _state.spawnQueue.Count > 0 
        ? (float)_state.spawnIndex / _state.spawnQueue.Count 
        : 1f;
        
      if (EditorUtility.DisplayCancelableProgressBar("Generating World",
            $"{_state.currentStatus}: {_state.spawnIndex}/{_state.spawnQueue.Count}",
            0.6f + progress * 0.4f)) {
        CancelGeneration();
        return;
      }

      if (_state.spawnIndex >= _state.spawnQueue.Count) {
        FinishGeneration();
      }
    }

    private static void SpawnActor(SpawnData data) {
      if (_container == null) return;

      var prefab = LoadPrefabByKey(data.actorKey);
      if (prefab == null) return;

      var groundedPos = CalculateGroundedPosition(data.position);
      var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _container);
      if (instance == null) return;

      instance.transform.position = groundedPos;
      instance.transform.rotation = Quaternion.Euler(0, data.rotation, 0);
      instance.transform.localScale = Vector3.one * data.scale;

      Undo.RegisterCreatedObjectUndo(instance, "Spawn Actor");
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

    private static void FinishGeneration() {
      EditorApplication.update -= OnEditorUpdate;
      EditorUtility.ClearProgressBar();

      Debug.Log($"[WorldGenEditor] ✓ Generated {_state.spawnIndex} actors");

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

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════════

    private static bool ValidatePlacement(BiomeScatterConfig sc, Vector3 pos) {
      if (!ValidateTerrainAt(sc, pos)) return false;
      if (!ValidateSpacing(sc.rule.minSpacing, pos, _state.spawnedPositions)) return false;
      
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

    private static bool ValidateSpacingRange(float minSpacing, Vector3 position, List<Vector3> spawned, int startIndex, int endIndex) {
      var sqrSpacing = minSpacing * minSpacing;
      for (var i = startIndex; i < endIndex && i < spawned.Count; i++) {
        if ((spawned[i] - position).sqrMagnitude < sqrSpacing) return false;
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

    private static GameObject LoadPrefabByKey(string actorKey) {
      if (string.IsNullOrWhiteSpace(actorKey)) return null;
      if (_prefabCache.TryGetValue(actorKey, out var cached)) return cached;

      var handle = Addressables.LoadAssetsAsync<GameObject>("Actors", null);
      var prefabs = handle.WaitForCompletion();

      foreach (var prefab in prefabs) {
        var actor = prefab.GetComponent<ActorDescription>();
        if (actor != null && actor.actorKey == actorKey) {
          _prefabCache[actorKey] = prefab;
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

    private static int CalculateTargetCount(ScatterRuleSO rule, Bounds bounds) {
      if (rule.fixedCount > 0) return rule.fixedCount;
      var area = bounds.size.x * bounds.size.z;
      return Mathf.RoundToInt(area / 100f * rule.density);
    }
  }
}
#endif
