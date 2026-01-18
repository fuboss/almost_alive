#if UNITY_EDITOR
using System.Collections.Generic;
using Content.Scripts.Game;
using Content.Scripts.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Content.Scripts.Editor.World {
  /// <summary>
  /// Edit-mode world generation for testing scatter rules without entering Play Mode.
  /// </summary>
  public static class WorldGeneratorEditor {
    private const string CONTAINER_NAME = "[EditorWorld_Generated]";
    private const float RAYCAST_HEIGHT = 50f;
    private const float RAYCAST_DISTANCE = 300f;
    private static readonly int GROUND_MASK = LayerMask.GetMask("Default");

    private static readonly Dictionary<string, GameObject> _prefabCache = new();
    private static Transform _container;

    [MenuItem("World/Generate (Edit Mode)")]
    public static void GenerateFromMenu() {
      var config = LoadConfig();
      if (config == null) return;
      Generate(config);
    }

    [MenuItem("World/Clear Generated")]
    public static void ClearFromMenu() {
      Clear();
    }

    public static void Generate(WorldGeneratorConfigSO config) {
      if (config == null) {
        Debug.LogError("[WorldGenEditor] No config provided");
        return;
      }

      Clear();
      _prefabCache.Clear();

      var terrain = config.terrain != null ? config.terrain : Terrain.activeTerrain;
      if (terrain == null) {
        Debug.LogError("[WorldGenEditor] No terrain found");
        return;
      }

      _container = new GameObject(CONTAINER_NAME).transform;
      Undo.RegisterCreatedObjectUndo(_container.gameObject, "Generate World");

      var seed = config.seed != 0 ? config.seed : System.Environment.TickCount;
      Random.InitState(seed);

      var totalSpawned = 0;
      foreach (var rule in config.scatterRules) {
        if (rule == null) continue;
        var spawned = GenerateFromRule(rule, terrain, config.edgeMargin);
        totalSpawned += spawned;
        if (config.logGeneration) {
          Debug.Log($"[WorldGenEditor] {rule.actorKey}: {spawned} spawned");
        }
      }

      Debug.Log($"[WorldGenEditor] Generated {totalSpawned} actors (seed: {seed})");
      _prefabCache.Clear();
    }

    public static void Clear() {
      var existing = GameObject.Find(CONTAINER_NAME);
      if (existing != null) {
        Undo.DestroyObjectImmediate(existing);
        Debug.Log("[WorldGenEditor] Cleared generated world");
      }

      _container = null;
    }

    /// <summary>
    /// Generate single rule for testing. Uses active terrain.
    /// </summary>
    public static void GenerateSingleRule(ScatterRuleSO rule) {
      if (rule == null) return;

      Clear();
      _prefabCache.Clear();

      var terrain = Terrain.activeTerrain;
      if (terrain == null) {
        Debug.LogError("[WorldGenEditor] No active terrain found");
        return;
      }

      _container = new GameObject(CONTAINER_NAME).transform;
      Undo.RegisterCreatedObjectUndo(_container.gameObject, "Generate Single Rule");

      var seed = System.Environment.TickCount;
      Random.InitState(seed);

      var spawned = GenerateFromRule(rule, terrain, 10f);
      Debug.Log($"[WorldGenEditor] {rule.actorKey}: {spawned} spawned (seed: {seed})");

      _prefabCache.Clear();
    }

    private static WorldGeneratorConfigSO LoadConfig() {
      var config = Resources.Load<WorldGeneratorConfigSO>("Environment/WorldGeneratorConfig");
      if (config == null) {
        Debug.LogError("[WorldGenEditor] WorldGeneratorConfig not found at Resources/Environment/");
      }

      return config;
    }

    private static int GenerateFromRule(ScatterRuleSO rule, Terrain terrain, float edgeMargin) {
      var prefab = LoadPrefab(rule.actorKey);
      if (prefab == null) {
        Debug.LogWarning($"[WorldGenEditor] Prefab '{rule.actorKey}' not found");
        return 0;
      }

      var bounds = GetTerrainBounds(terrain, edgeMargin);
      var targetCount = CalculateTargetCount(rule, bounds);
      var spawned = new List<Vector3>();
      var placed = 0;

      if (rule.useClustering) {
        placed = GenerateClustered(rule, prefab, terrain, bounds, targetCount, spawned);
      }
      else {
        placed = GenerateUniform(rule, prefab, terrain, bounds, targetCount, spawned);
      }

      return placed;
    }

    private static int GenerateUniform(ScatterRuleSO rule, GameObject prefab, Terrain terrain,
      Bounds bounds, int targetCount, List<Vector3> spawned) {
      var placed = 0;
      var attempts = 0;
      var maxAttempts = targetCount * rule.maxAttempts;

      while (placed < targetCount && attempts < maxAttempts) {
        attempts++;
        var pos = RandomPointInBounds(bounds);

        if (!ValidatePlacement(rule, terrain, pos, spawned)) continue;

        SpawnActor(prefab, rule, pos);
        spawned.Add(pos);
        placed++;
      }

      return placed;
    }

    private static int GenerateClustered(ScatterRuleSO rule, GameObject prefab, Terrain terrain,
      Bounds bounds, int targetCount, List<Vector3> spawned) {
      var placed = 0;
      var remaining = targetCount;
      var clusterAttempts = 0;
      var maxClusterAttempts = targetCount * 10;

      while (remaining > 0 && clusterAttempts < maxClusterAttempts) {
        clusterAttempts++;
        var clusterCenter = RandomPointInBounds(bounds);

        if (!ValidateTerrainAt(rule, terrain, clusterCenter)) continue;

        var clusterCount = Random.Range(rule.clusterSize.x, rule.clusterSize.y + 1);
        clusterCount = Mathf.Min(clusterCount, remaining);

        for (var i = 0; i < clusterCount; i++) {
          var offset = Random.insideUnitCircle * rule.clusterSpread;
          var pos = clusterCenter + new Vector3(offset.x, 0, offset.y);

          if (!ValidatePlacement(rule, terrain, pos, spawned)) continue;

          SpawnActor(prefab, rule, pos);
          spawned.Add(pos);
          placed++;
          remaining--;
        }
      }

      return placed;
    }

    private static void SpawnActor(GameObject prefab, ScatterRuleSO rule, Vector3 position) {
      var groundedPos = CalculateGroundedPosition(position, prefab);
      var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _container);
      instance.transform.position = groundedPos;

      if (rule.randomRotation) {
        instance.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
      }

      var scale = Random.Range(rule.scaleRange.x, rule.scaleRange.y);
      instance.transform.localScale = Vector3.one * scale;

      Undo.RegisterCreatedObjectUndo(instance, "Spawn Actor");
    }

    private static bool ValidatePlacement(ScatterRuleSO rule, Terrain terrain, Vector3 pos, List<Vector3> spawned) {
      if (!ValidateTerrainAt(rule, terrain, pos)) return false;
      if (!ValidateSpacing(rule, pos, spawned)) return false;
      return true;
    }

    private static bool ValidateTerrainAt(ScatterRuleSO rule, Terrain terrain, Vector3 worldPos) {
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

        if (System.Array.IndexOf(rule.allowedTerrainLayers, dominantLayer) < 0)
          return false;
      }

      return true;
    }

    private static bool ValidateSpacing(ScatterRuleSO rule, Vector3 position, List<Vector3> spawned) {
      var sqrSpacing = rule.minSpacing * rule.minSpacing;
      foreach (var pos in spawned) {
        if ((pos - position).sqrMagnitude < sqrSpacing)
          return false;
      }

      return true;
    }

    private static GameObject LoadPrefab(string actorKey) {
      if (_prefabCache.TryGetValue(actorKey, out var cached)) return cached;

      // Load all actors and find by key
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

    private static Vector3 CalculateGroundedPosition(Vector3 targetPos, GameObject prefab) {
      var rayOrigin = new Vector3(targetPos.x, targetPos.y + RAYCAST_HEIGHT, targetPos.z);

      if (!Physics.Raycast(rayOrigin, Vector3.down, out var hit, RAYCAST_DISTANCE, GROUND_MASK)) {
        return targetPos;
      }

      var groundY = hit.point.y;
      var boundsOffset = GetBoundsOffsetY(prefab);
      return new Vector3(targetPos.x, groundY + boundsOffset, targetPos.z);
    }

    private static float GetBoundsOffsetY(GameObject prefab) {
      // var collider = prefab.GetComponentInChildren<Collider>();
      // if (collider != null) {
      //   var bounds = collider.bounds;
      //   var localMin = prefab.transform.InverseTransformPoint(bounds.min);
      //   return -localMin.y;
      // }
      //
      // var renderer = prefab.GetComponentInChildren<Renderer>();
      // if (renderer != null) {
      //   var bounds = renderer.bounds;
      //   var localMin = prefab.transform.InverseTransformPoint(bounds.min);
      //   return -localMin.y;
      // }

      return 0f;
    }

    private static Bounds GetTerrainBounds(Terrain terrain, float edgeMargin) {
      var pos = terrain.transform.position;
      var size = terrain.terrainData.size;

      return new Bounds(
        pos + size * 0.5f,
        new Vector3(size.x - edgeMargin * 2, size.y, size.z - edgeMargin * 2)
      );
    }

    private static Vector3 RandomPointInBounds(Bounds bounds) {
      return new Vector3(
        Random.Range(bounds.min.x, bounds.max.x),
        0,
        Random.Range(bounds.min.z, bounds.max.z)
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