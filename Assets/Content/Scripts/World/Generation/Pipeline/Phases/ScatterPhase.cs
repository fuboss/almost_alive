using System.Collections.Generic;
using Content.Scripts.Game;
using Content.Scripts.World.Biomes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Content.Scripts.World.Generation.Pipeline.Phases {
  /// <summary>
  /// Phase 5: Spawn scatter objects (trees, rocks, actors).
  /// Uses Poisson disk sampling for natural distribution.
  /// Loads prefabs via Addressables by actorKey.
  /// </summary>
  public class ScatterPhase : GenerationPhaseBase {
    
    public override string Name => "Scatters";
    public override string Description => "Spawn trees, rocks, objects";

    // Track spawned objects for rollback
    private readonly List<GameObject> _spawnedObjects = new();
    private readonly HashSet<string> _warnedMissingActors = new();
    private readonly Dictionary<BiomeType, Transform> _biomeContainers = new();
    private Transform _scatterRoot;
    
    // Addressables cache
    private Dictionary<string, GameObject> _prefabCache;
    private IList<GameObject> _loadedActorPrefabs;
    private AsyncOperationHandle<IList<GameObject>> _actorsHandle;
    
    // Phase-specific random for deterministic results
    private System.Random _phaseRandom;
    private const int PHASE_INDEX = 4; // Scatter is phase 5 (0-indexed)

    protected override bool ValidateContext(GenerationContext ctx) {
      if (ctx?.BiomeMap == null) {
        Debug.LogError("[Scatter] BiomeMap not generated");
        return false;
      }
      
      #if UNITY_EDITOR
      if (!Application.isPlaying && !ctx.Config.createScattersInEditor) {
        Debug.Log("[Scatter] Skipped (createScattersInEditor disabled)");
        return false;
      }
      #endif
      
      return true;
    }

    protected override void ExecuteInternal(GenerationContext ctx) {
      ReportProgress(0f, "Loading actor prefabs...");
      
      _spawnedObjects.Clear();
      _warnedMissingActors.Clear();
      _biomeContainers.Clear();
      
      // Create deterministic random for this phase
      _phaseRandom = ctx.CreatePhaseRandom(PHASE_INDEX);
      
      // Cleanup any existing scatter root from previous runs
      CleanupExistingScatters();
      
      // Load all actor prefabs via Addressables (sync for editor)
      LoadActorPrefabs();
      
      if (_loadedActorPrefabs == null || _loadedActorPrefabs.Count == 0) {
        Debug.LogWarning("[Scatter] No actor prefabs loaded from Addressables label 'Actors'");
        return;
      }
      
      LogDebug(ctx, $"[Scatter] Loaded {_loadedActorPrefabs.Count} actor prefabs");
      
      // Create root object for organization
      _scatterRoot = new GameObject("[Generated_Scatters]").transform;
      
      var biomeMap = ctx.BiomeMap;
      var config = ctx.Config;
      var bounds = ctx.Bounds;
      var terrain = ctx.Terrain;
      
      // Collect all scatter rules from all biomes
      var totalScatters = 0;
      foreach (var biome in config.biomes) {
        if (biome?.scatterConfigs != null) {
          totalScatters += biome.scatterConfigs.Count;
        }
      }
      
      var processedScatters = 0;
      
      // Process each biome
      foreach (var biome in config.biomes) {
        if (biome?.scatterConfigs == null) continue;
        
        foreach (var scatterConfig in biome.scatterConfigs) {
          if (scatterConfig?.rule == null) continue;
          
          processedScatters++;
          var progress = (float)processedScatters / Mathf.Max(1, totalScatters);
          ReportProgress(progress * 0.9f, $"Spawning {scatterConfig.rule.actorName}...");
          
          // Generate spawn points using Poisson disk sampling
          var rule = scatterConfig.rule;
          var points = GenerateScatterPoints(
            bounds, 
            biome, 
            biomeMap, 
            rule.minSpacing, 
            rule.density,
            ctx
          );
          
          // Spawn objects at points
          var spawnedCount = 0;
          var biomeContainer = GetOrCreateBiomeContainer(biome.type);
          
          foreach (var point in points) {
            var obj = SpawnScatterObject(rule, point, terrain, ctx);
            if (obj != null) {
              obj.transform.SetParent(biomeContainer);
              _spawnedObjects.Add(obj);
              spawnedCount++;
            }
          }
          
          LogDebug(ctx, $"[Scatter] {rule.actorName}: {points.Count} points → {spawnedCount} spawned");
        }
      }
      
      ReportProgress(1f);
      ClearProgressBar();
      
      LogDebug(ctx, $"[Scatter] Total spawned: {_spawnedObjects.Count} objects");
      
      // Release Addressables handle
      ReleaseActorPrefabs();
    }

    protected override void RollbackInternal(GenerationContext ctx) {
      // Destroy all spawned objects
      foreach (var obj in _spawnedObjects) {
        if (obj != null) {
          #if UNITY_EDITOR
          Object.DestroyImmediate(obj);
          #else
          Object.Destroy(obj);
          #endif
        }
      }
      _spawnedObjects.Clear();
      _biomeContainers.Clear();
      
      // Destroy root
      if (_scatterRoot != null) {
        #if UNITY_EDITOR
        Object.DestroyImmediate(_scatterRoot.gameObject);
        #else
        Object.Destroy(_scatterRoot.gameObject);
        #endif
        _scatterRoot = null;
      }
      
      // Cleanup addressables
      ReleaseActorPrefabs();
    }

    private void CleanupExistingScatters() {
      // Find and destroy any existing scatter root (from previous generation)
      var existing = GameObject.Find("[Generated_Scatters]");
      if (existing != null) {
        #if UNITY_EDITOR
        Object.DestroyImmediate(existing);
        #else
        Object.Destroy(existing);
        #endif
        Debug.Log("[Scatter] Cleaned up existing scatter root");
      }
      _scatterRoot = null;
      _biomeContainers.Clear();
    }

    private Transform GetOrCreateBiomeContainer(BiomeType biomeType) {
      if (_biomeContainers.TryGetValue(biomeType, out var container)) {
        return container;
      }
      
      var go = new GameObject($"[{biomeType}]");
      go.transform.SetParent(_scatterRoot, false);
      _biomeContainers[biomeType] = go.transform;
      return go.transform;
    }

    private void LoadActorPrefabs() {
      if (_loadedActorPrefabs != null) return;
      
      _prefabCache = new Dictionary<string, GameObject>();
      
      #if UNITY_EDITOR
      // Sync load for editor mode
      _actorsHandle = Addressables.LoadAssetsAsync<GameObject>("Actors", null);
      _loadedActorPrefabs = _actorsHandle.WaitForCompletion();
      
      // Build cache by actorKey
      foreach (var prefab in _loadedActorPrefabs) {
        var actor = prefab.GetComponent<ActorDescription>();
        if (actor != null && !string.IsNullOrEmpty(actor.actorKey)) {
          _prefabCache[actor.actorKey] = prefab;
        }
      }
      #endif
    }

    private void ReleaseActorPrefabs() {
      if (_loadedActorPrefabs != null) {
        Addressables.Release(_actorsHandle);
        _loadedActorPrefabs = null;
      }
      _prefabCache?.Clear();
      _prefabCache = null;
    }

    private GameObject GetPrefabByKey(string actorKey) {
      if (string.IsNullOrEmpty(actorKey)) return null;
      if (_prefabCache == null) return null;
      
      return _prefabCache.GetValueOrDefault(actorKey);
    }

    private List<Vector3> GenerateScatterPoints(
      Bounds bounds, 
      BiomeSO targetBiome,
      BiomeMap biomeMap,
      float minSpacing,
      float density,
      GenerationContext ctx)
    {
      var points = new List<Vector3>();
      
      // Simple grid-based sampling with rejection
      // TODO: Replace with proper Poisson disk sampling
      var cellSize = minSpacing;
      var gridW = Mathf.CeilToInt(bounds.size.x / cellSize);
      var gridH = Mathf.CeilToInt(bounds.size.z / cellSize);
      
      for (int gx = 0; gx < gridW; gx++) {
        for (int gz = 0; gz < gridH; gz++) {
          // Random position within cell
          var x = bounds.min.x + (gx + RandomValue()) * cellSize;
          var z = bounds.min.z + (gz + RandomValue()) * cellSize;
          var pos = new Vector3(x, 0, z);
          
          // Check if position is in correct biome (compare BiomeType)
          var posBiomeType = biomeMap.GetBiomeAt(pos);
          if (posBiomeType != targetBiome.type) continue;
          
          // Density check
          if (RandomValue() > density) continue;
          
          points.Add(pos);
        }
      }
      
      return points;
    }

    private GameObject SpawnScatterObject(
      ScatterRuleSO rule, 
      Vector3 position, 
      Terrain terrain,
      GenerationContext ctx)
    {
      // Get prefab by actorKey
      var prefab = GetPrefabByKey(rule.actorKey);
      if (prefab == null) {
        // Log warning only once per actorKey
        if (!_warnedMissingActors.Contains(rule.actorKey)) {
          _warnedMissingActors.Add(rule.actorKey);
          Debug.LogWarning($"[Scatter] Actor '{rule.actorKey}' not found in Addressables. Check label 'Actors' and ActorDescription.actorKey.");
        }
        return null;
      }
      
      // Sample terrain height
      var terrainHeight = terrain.SampleHeight(position);
      position.y = terrain.transform.position.y + terrainHeight;
      
      // Random rotation
      var rotation = Quaternion.Euler(0, RandomRange(0f, 360f), 0);
      
      // Random scale
      var scale = RandomRange(rule.scaleRange.x, rule.scaleRange.y);
      
      // Instantiate
      #if UNITY_EDITOR
      var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
      #else
      var obj = Object.Instantiate(prefab);
      #endif
      
      obj.transform.position = position;
      obj.transform.rotation = rotation;
      obj.transform.localScale = Vector3.one * scale;
      
      return obj;
    }

    // ═══════════════════════════════════════════════════════════════
    // RANDOM HELPERS (use phase-specific random)
    // ═══════════════════════════════════════════════════════════════

    private float RandomValue() => (float)_phaseRandom.NextDouble();
    private float RandomRange(float min, float max) => min + (float)_phaseRandom.NextDouble() * (max - min);
  }
}
