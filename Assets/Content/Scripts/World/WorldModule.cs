using System;
using System.Collections.Generic;
using System.Threading;
using Content.Scripts.AI.GOAP;
using Content.Scripts.Game;
using Content.Scripts.World.Biomes;
using Content.Scripts.World.Vegetation;
using Cysharp.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.World {
  /// <summary>
  /// Runtime world generation with full biome pipeline.
  /// Two-phase generation for determinism: positions first, then transforms.
  /// </summary>
  public class WorldModule : IInitializable, IDisposable {
    private const int SPAWN_MAX_PER_FRAME = 10;
    private const string CONTAINER_NAME = "[World_Generated]";

    private int _spawnedThisFrame;
    private Transform _container;

    [Inject] private readonly ActorCreationModule _actorCreation;
    [Inject] private readonly WorldSaveModule _saveModule;

    private WorldGeneratorConfigSO _config;
    private Terrain _terrain;
    private BiomeMap _biomeMap;
    private TerrainFeatureMap _featureMap;
    private readonly List<ActorDescription> _spawnedActors = new();
    private CancellationTokenSource _generationCts;
    
    private WorldRandom _positionRandom;
    private WorldRandom _transformRandom;

    public IReadOnlyList<ActorDescription> spawnedActors => _spawnedActors;
    public BiomeMap biomeMap => _biomeMap;
    public TerrainFeatureMap featureMap => _featureMap;
    public bool isGenerated { get; private set; }
    public bool isGenerating { get; private set; }
    public float generationProgress { get; private set; }

    public event Action<float> OnGenerationProgress;
    public event Action OnGenerationComplete;

    // Pre-generated spawn data
    private struct SpawnData {
      public string actorKey;
      public Vector3 position;
      public float rotation;
      public float scale;
    }

    void IInitializable.Initialize() {
      _config = Resources.Load<WorldGeneratorConfigSO>("Environment/WorldGeneratorConfig");
      if (_config == null) {
        Debug.LogWarning("[WorldModule] No WorldGeneratorConfig found");
        return;
      }

      _terrain = _config.terrain != null ? _config.terrain : Terrain.activeTerrain;
      if (_terrain == null) {
        Debug.LogError("[WorldModule] No terrain found");
        return;
      }

      if (_saveModule != null && _saveModule.HasSave()) {
        Debug.Log("[WorldModule] Found existing save, skipping generation");
        return;
      }

      GenerateAsync().Forget();
    }

    private async UniTask GenerateAsync(CancellationToken externalCt = default) {
      if (_config == null || _terrain == null) return;
      if (isGenerating) return;

      if (_config.biomes == null || _config.biomes.Count == 0) {
        Debug.LogError("[WorldModule] No biomes configured");
        return;
      }

      Clear();
      await UniTask.WaitForSeconds(1f, cancellationToken: externalCt);
      SetTerrainFromConfig();
      await UniTask.WaitUntil(_actorCreation, c => c.IsInitialized, cancellationToken: externalCt);

      _generationCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
      var ct = _generationCts.Token;

      isGenerating = true;
      generationProgress = 0f;

      var seed = _config.seed != 0 ? _config.seed : Environment.TickCount;
      _positionRandom = new WorldRandom(seed);
      _transformRandom = new WorldRandom(seed + 1000);
      if (_config.logGeneration) Debug.Log($"[WorldModule] Generating with seed {seed}");

      var bounds = GetTerrainBounds();

      // Phase 1: Generate Biomes
      UpdateProgress(0.05f);
      _biomeMap = VoronoiGenerator.Generate(
        bounds, _config.biomes, _config.biomeBorderBlend, seed,
        _config.minBiomeCells, _config.maxBiomeCells
      );

      if (_biomeMap == null) {
        Debug.LogError("[WorldModule] Failed to generate biome map");
        isGenerating = false;
        return;
      }

      _config.cachedBiomeMap = _biomeMap;

      // Phase 2: Sculpt Terrain
      if (_config.sculptTerrain) {
        UpdateProgress(0.15f);
        TerrainSculptor.Sculpt(_terrain, _biomeMap, seed);
        await UniTask.Yield(ct);
      }

      // Phase 3: Paint Splatmap
      if (_config.paintSplatmap) {
        UpdateProgress(0.25f);
        SplatmapPainter.Paint(_terrain, _biomeMap, seed);
        await UniTask.Yield(ct);
      }

      // Phase 3.5: Generate Feature Map
      UpdateProgress(0.28f);
      _featureMap = TerrainFeatureMap.Generate(_terrain);
      _config.cachedFeatureMap = _featureMap;

      // Phase 3.6: Paint Vegetation
      if (_config.paintVegetation) {
        UpdateProgress(0.32f);
        VegetationPainter.Paint(_terrain, _biomeMap, _config.biomes, seed);
        await UniTask.Yield(ct);
      }

      // Phase 4: Generate ALL positions first (positionRandom only)
      UpdateProgress(0.30f);
      var allPositions = new List<(string actorKey, Vector3 position, ScatterRuleSO rule)>();
      var spawnedPositions = new List<Vector3>();

      foreach (var biome in _config.biomes) {
        if (biome?.scatterConfigs == null) continue;
        if (ct.IsCancellationRequested) break;

        foreach (var sc in biome.scatterConfigs) {
          if (sc?.rule == null) continue;
          if (ct.IsCancellationRequested) break;

          var rule = sc.rule;
          var targetCount = CalculateTargetCount(rule, bounds);

          if (_config.logGeneration) {
            Debug.Log($"[WorldModule] {rule.actorName}: target={targetCount}, clustering={rule.useClustering}");
          }

          if (rule.useClustering) {
            GenerateClusteredPositions(sc, biome.type, bounds, targetCount, allPositions, spawnedPositions);
          } else {
            GenerateUniformPositions(sc, biome.type, bounds, targetCount, allPositions, spawnedPositions);
          }

          if (_config.logGeneration) {
            Debug.Log($"[WorldModule] Positions: {biome.type}/{rule.actorName}");
          }
        }
      }

      if (_config.logGeneration) Debug.Log($"[WorldModule] Positions: {allPositions.Count}");

      // Phase 5: Generate transforms for all positions (transformRandom only)
      UpdateProgress(0.50f);
      var allSpawnData = new List<SpawnData>(allPositions.Count);
      
      foreach (var (actorKey, position, rule) in allPositions) {
        var rotation = rule.randomRotation ? _transformRandom.Range(0f, 360f) : 0f;
        var scale = _transformRandom.Range(rule.scaleRange.x, rule.scaleRange.y);
        
        allSpawnData.Add(new SpawnData {
          actorKey = actorKey,
          position = position,
          rotation = rotation,
          scale = scale
        });
      }

      // Phase 6: Spawn all actors
      UpdateProgress(0.60f);
      _spawnedThisFrame = 0;
      
      for (var i = 0; i < allSpawnData.Count; i++) {
        if (ct.IsCancellationRequested) break;
        
        var data = allSpawnData[i];
        if (_actorCreation.TrySpawnActor(data.actorKey, data.position, out var actor)) {
          actor.transform.SetParent(GetOrCreateContainer(), true);
          actor.transform.rotation = Quaternion.Euler(0, data.rotation, 0);
          actor.transform.localScale = Vector3.one * data.scale;
          _spawnedActors.Add(actor);
        }

        UpdateProgress(0.60f + 0.40f * i / Mathf.Max(1, allSpawnData.Count));
        await YieldIfNeeded(ct);
      }

      // Done
      isGenerating = false;
      isGenerated = !ct.IsCancellationRequested;
      generationProgress = 1f;

      if (isGenerated) {
        var navSurface = _terrain.GetComponent<NavMeshSurface>();
        if (navSurface != null) navSurface.BuildNavMesh();

        if (_config.logGeneration) Debug.Log($"[WorldModule] ✓ Generated {_spawnedActors.Count} actors");
        OnGenerationComplete?.Invoke();
      }

      _generationCts?.Dispose();
      _generationCts = null;
    }

    // ═══════════════════════════════════════════════════════════════
    // POSITION GENERATION (positionRandom only)
    // ═══════════════════════════════════════════════════════════════

    private void GenerateUniformPositions(BiomeScatterConfig sc, BiomeType biomeType, Bounds bounds,
      int targetCount, List<(string, Vector3, ScatterRuleSO)> output, List<Vector3> spawnedPositions) {
      var rule = sc.rule;
      var placed = 0;
      var attempts = 0;
      var maxAttempts = targetCount * rule.maxAttempts;

      while (placed < targetCount && attempts < maxAttempts) {
        attempts++;

        var pos = _positionRandom.RandomPointInBounds(bounds);
        if (_biomeMap.GetBiomeAt(pos) != biomeType) continue;
        if (!ValidatePlacement(sc, pos, spawnedPositions)) continue;

        output.Add((rule.actorKey, pos, rule));
        spawnedPositions.Add(pos);
        placed++;

        if (rule.hasChildren) {
          GenerateChildPositions(rule, pos, output, spawnedPositions);
        }
      }
    }

    private void GenerateClusteredPositions(BiomeScatterConfig sc, BiomeType biomeType, Bounds bounds,
      int targetCount, List<(string, Vector3, ScatterRuleSO)> output, List<Vector3> spawnedPositions) {
      var rule = sc.rule;
      var remaining = targetCount;
      var clusterAttempts = 0;
      var maxClusterAttempts = targetCount * 10;

      while (remaining > 0 && clusterAttempts < maxClusterAttempts) {
        clusterAttempts++;

        var clusterCenter = _positionRandom.RandomPointInBounds(bounds);
        if (_biomeMap.GetBiomeAt(clusterCenter) != biomeType) continue;
        if (!ValidateTerrainAt(sc, clusterCenter)) continue;

        var clusterCount = Mathf.Min(
          _positionRandom.Range(rule.clusterSize.x, rule.clusterSize.y + 1),
          remaining
        );

        // Track positions within THIS cluster only
        var clusterLocalPositions = new List<Vector3>();
        var clusterStartIndex = spawnedPositions.Count;

        for (var i = 0; i < clusterCount; i++) {
          var offset = _positionRandom.InsideUnitCircle() * rule.clusterSpread;
          var pos = clusterCenter + new Vector3(offset.x, 0, offset.y);

          if (_biomeMap.GetBiomeAt(pos) != biomeType) continue;
          if (!ValidateTerrainAt(sc, pos)) continue;
          if (!ValidateSpacingRange(rule.minSpacing, pos, spawnedPositions, 0, clusterStartIndex)) continue;
          if (!ValidateLocalSpacing(rule.minSpacing * 0.3f, pos, clusterLocalPositions)) continue;

          output.Add((rule.actorKey, pos, rule));
          spawnedPositions.Add(pos);
          clusterLocalPositions.Add(pos);
          remaining--;

          if (rule.hasChildren) {
            GenerateChildPositions(rule, pos, output, spawnedPositions);
          }
        }
      }
    }

    private void GenerateChildPositions(ScatterRuleSO parentRule, Vector3 parentPos,
      List<(string, Vector3, ScatterRuleSO)> output, List<Vector3> spawnedPositions, int depth = 0) {
      const int MAX_DEPTH = 3;
      if (depth >= MAX_DEPTH || parentRule.childScatters == null) return;

      foreach (var childConfig in parentRule.childScatters) {
        if (childConfig?.rule == null) continue;

        var childRule = childConfig.rule;
        var count = _positionRandom.Range(childConfig.countPerParent.x, childConfig.countPerParent.y + 1);
        var localSpawned = new List<Vector3>();

        for (var i = 0; i < count; i++) {
          var attempts = 0;
          while (attempts < childRule.maxAttempts) {
            attempts++;

            var angle = _positionRandom.Range(0f, Mathf.PI * 2f);
            var radius = _positionRandom.Range(childConfig.radiusMin, childConfig.radiusMax);
            var pos = parentPos + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            if (!ValidateTerrainAtRule(childRule, pos)) continue;
            if (childConfig.inheritTerrainFilter && !ValidateTerrainAtRule(parentRule, pos)) continue;

            var spacing = childConfig.localSpacingOnly
              ? ValidateLocalSpacing(childRule.minSpacing, pos, localSpawned)
              : ValidateSpacingList(childRule.minSpacing, pos, spawnedPositions);

            if (!spacing) continue;

            output.Add((childRule.actorKey, pos, childRule));
            localSpawned.Add(pos);
            spawnedPositions.Add(pos);

            if (childRule.hasChildren) {
              GenerateChildPositions(childRule, pos, output, spawnedPositions, depth + 1);
            }
            break;
          }
        }
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private void SetTerrainFromConfig() {
      _terrain.terrainData.size = new Vector3(_config.size, 200, _config.size);
      _terrain.transform.localPosition = new Vector3(-_config.size / 2f, 0, -_config.size / 2f);
    }

    public void CancelGeneration() => _generationCts?.Cancel();

    public void Clear() {
      CancelGeneration();
      foreach (var actor in _spawnedActors) {
        if (actor != null) UnityEngine.Object.Destroy(actor.gameObject);
      }
      _spawnedActors.Clear();

      if (_container != null) {
        UnityEngine.Object.Destroy(_container.gameObject);
        _container = null;
      }

      // Clear vegetation
      if (_terrain != null) {
        VegetationPainter.Clear(_terrain);
      }

      _biomeMap = null;
      _featureMap = null;
      _positionRandom = null;
      _transformRandom = null;
      isGenerated = false;
      generationProgress = 0f;
    }

    public BiomeType? GetBiomeAt(Vector3 worldPos) => _biomeMap?.GetBiomeAt(worldPos);
    public BiomeSO GetBiomeDataAt(Vector3 worldPos) => _biomeMap?.GetBiomeDataAt(worldPos);

    private Transform GetOrCreateContainer() {
      if (_container != null) return _container;
      var existing = GameObject.Find(CONTAINER_NAME);
      _container = existing != null ? existing.transform : new GameObject(CONTAINER_NAME).transform;
      return _container;
    }

    private async UniTask YieldIfNeeded(CancellationToken ct) {
      _spawnedThisFrame++;
      if (_spawnedThisFrame >= SPAWN_MAX_PER_FRAME) {
        _spawnedThisFrame = 0;
        await UniTask.Yield(PlayerLoopTiming.Update, ct);
      }
    }

    private void UpdateProgress(float value) {
      generationProgress = value;
      OnGenerationProgress?.Invoke(generationProgress);
    }

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════════

    private bool ValidatePlacement(BiomeScatterConfig sc, Vector3 pos, List<Vector3> spawned) {
      if (!ValidateTerrainAt(sc, pos)) return false;
      if (!ValidateSpacingList(sc.rule.minSpacing, pos, spawned)) return false;
      
      if (sc.requiresFeatureMap && _featureMap != null) {
        if (!_featureMap.CheckPlacement(pos, sc.placement)) return false;
      }
      
      return true;
    }

    private bool ValidateTerrainAt(BiomeScatterConfig sc, Vector3 worldPos) {
      var terrainPos = _terrain.transform.position;
      var terrainData = _terrain.terrainData;
      var size = terrainData.size;

      var normalizedX = (worldPos.x - terrainPos.x) / size.x;
      var normalizedZ = (worldPos.z - terrainPos.z) / size.z;

      if (normalizedX < 0 || normalizedX > 1 || normalizedZ < 0 || normalizedZ > 1)
        return false;

      var height = _terrain.SampleHeight(worldPos);
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

    private bool ValidateTerrainAtRule(ScatterRuleSO rule, Vector3 worldPos) {
      var terrainPos = _terrain.transform.position;
      var terrainData = _terrain.terrainData;
      var size = terrainData.size;

      var normalizedX = (worldPos.x - terrainPos.x) / size.x;
      var normalizedZ = (worldPos.z - terrainPos.z) / size.z;

      if (normalizedX < 0 || normalizedX > 1 || normalizedZ < 0 || normalizedZ > 1)
        return false;

      var height = _terrain.SampleHeight(worldPos);
      if (height < rule.heightRange.x || height > rule.heightRange.y)
        return false;

      var slope = terrainData.GetSteepness(normalizedX, normalizedZ);
      if (slope < rule.slopeRange.x || slope > rule.slopeRange.y)
        return false;

      return true;
    }

    private bool ValidateSpacingList(float minSpacing, Vector3 position, List<Vector3> spawned) {
      var sqrSpacing = minSpacing * minSpacing;
      foreach (var pos in spawned) {
        if ((pos - position).sqrMagnitude < sqrSpacing) return false;
      }
      return true;
    }

    private bool ValidateSpacingRange(float minSpacing, Vector3 position, List<Vector3> spawned, int startIndex, int endIndex) {
      var sqrSpacing = minSpacing * minSpacing;
      for (var i = startIndex; i < endIndex && i < spawned.Count; i++) {
        if ((spawned[i] - position).sqrMagnitude < sqrSpacing) return false;
      }
      return true;
    }

    private bool ValidateLocalSpacing(float minSpacing, Vector3 position, List<Vector3> siblings) {
      var sqrSpacing = minSpacing * minSpacing;
      foreach (var pos in siblings) {
        if ((pos - position).sqrMagnitude < sqrSpacing) return false;
      }
      return true;
    }

    private Bounds GetTerrainBounds() {
      var pos = _terrain.transform.position;
      var size = _terrain.terrainData.size;
      return new Bounds(
        pos + size * 0.5f,
        new Vector3(size.x - _config.edgeMargin * 2, size.y, size.z - _config.edgeMargin * 2)
      );
    }

    private int CalculateTargetCount(ScatterRuleSO rule, Bounds bounds) {
      if (rule.fixedCount > 0) return rule.fixedCount;
      var area = bounds.size.x * bounds.size.z;
      return Mathf.RoundToInt(area / 100f * rule.density);
    }

    public void Dispose() {
      CancelGeneration();
      _spawnedActors.Clear();
    }
  }
}
