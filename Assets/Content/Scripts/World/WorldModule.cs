using System;
using System.Collections.Generic;
using System.Threading;
using Content.Scripts.AI.GOAP;
using Content.Scripts.Game;
using Content.Scripts.World.Biomes;
using Cysharp.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Random = UnityEngine.Random;

namespace Content.Scripts.World {
  /// <summary>
  /// Runtime world generation with full biome pipeline.
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
    private readonly List<ActorDescription> _spawnedActors = new();
    private CancellationTokenSource _generationCts;

    public IReadOnlyList<ActorDescription> spawnedActors => _spawnedActors;
    public BiomeMap biomeMap => _biomeMap;
    public bool isGenerated { get; private set; }
    public bool isGenerating { get; private set; }
    public float generationProgress { get; private set; }

    public event Action<float> OnGenerationProgress;
    public event Action OnGenerationComplete;

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
      Random.InitState(seed);
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

      // Phase 4: Spawn Scatters
      UpdateProgress(0.30f);

      var totalTarget = 0;
      foreach (var biome in _config.biomes) {
        if (biome?.scatterConfigs == null) continue;
        foreach (var sc in biome.scatterConfigs) {
          if (sc?.rule != null) totalTarget += CalculateTargetCount(sc.rule, bounds);
        }
      }

      var totalPlaced = 0;
      _spawnedThisFrame = 0;

      foreach (var biome in _config.biomes) {
        if (biome?.scatterConfigs == null) continue;
        if (ct.IsCancellationRequested) break;

        foreach (var sc in biome.scatterConfigs) {
          if (sc?.rule == null) continue;
          if (ct.IsCancellationRequested) break;

          var rule = sc.rule;
          var targetCount = CalculateTargetCount(rule, bounds);
          int placed;

          if (rule.useClustering) {
            placed = await GenerateClusteredAsync(sc, biome.type, bounds, targetCount, ct,
              p => UpdateProgress(0.3f + 0.7f * (totalPlaced + p) / Mathf.Max(1, totalTarget)));
          } else {
            placed = await GenerateUniformAsync(sc, biome.type, bounds, targetCount, ct,
              p => UpdateProgress(0.3f + 0.7f * (totalPlaced + p) / Mathf.Max(1, totalTarget)));
          }

          totalPlaced += placed;

          if (_config.logGeneration) {
            Debug.Log($"[WorldModule] {biome.type}/{rule.actorName} ({sc.placement}): {placed}/{targetCount}");
          }
        }
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

      _biomeMap = null;
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

    private async UniTask<int> GenerateUniformAsync(BiomeScatterConfig sc, BiomeType biomeType, Bounds bounds,
      int targetCount, CancellationToken ct, Action<int> onProgress) {
      var rule = sc.rule;
      var placed = 0;
      var attempts = 0;
      var maxAttempts = targetCount * rule.maxAttempts;

      while (placed < targetCount && attempts < maxAttempts) {
        if (ct.IsCancellationRequested) break;
        attempts++;

        var pos = RandomPointInBounds(bounds);
        if (_biomeMap.GetBiomeAt(pos) != biomeType) continue;

        if (TryPlaceActor(sc, pos, out var actor)) {
          _spawnedActors.Add(actor);
          placed++;
          onProgress?.Invoke(placed);

          if (rule.hasChildren) {
            await SpawnChildrenAroundAsync(rule, actor.transform.position, ct);
          }
          await YieldIfNeeded(ct);
        }
      }
      return placed;
    }

    private async UniTask<int> GenerateClusteredAsync(BiomeScatterConfig sc, BiomeType biomeType, Bounds bounds,
      int targetCount, CancellationToken ct, Action<int> onProgress) {
      var rule = sc.rule;
      var placed = 0;
      var remaining = targetCount;
      var clusterAttempts = 0;
      var maxClusterAttempts = targetCount * 10;

      while (remaining > 0 && clusterAttempts < maxClusterAttempts) {
        if (ct.IsCancellationRequested) break;
        clusterAttempts++;

        var clusterCenter = RandomPointInBounds(bounds);
        if (_biomeMap.GetBiomeAt(clusterCenter) != biomeType) continue;
        if (!ValidateTerrainAt(sc, clusterCenter)) continue;

        var clusterCount = Mathf.Min(
          Random.Range(rule.clusterSize.x, rule.clusterSize.y + 1),
          remaining
        );

        for (var i = 0; i < clusterCount; i++) {
          if (ct.IsCancellationRequested) break;

          var offset = Random.insideUnitCircle * rule.clusterSpread;
          var pos = clusterCenter + new Vector3(offset.x, 0, offset.y);

          if (_biomeMap.GetBiomeAt(pos) != biomeType) continue;

          if (TryPlaceActor(sc, pos, out var actor)) {
            _spawnedActors.Add(actor);
            placed++;
            remaining--;
            onProgress?.Invoke(placed);

            if (rule.hasChildren) {
              await SpawnChildrenAroundAsync(rule, actor.transform.position, ct);
            }
            await YieldIfNeeded(ct);
          }
        }
      }
      return placed;
    }

    private async UniTask SpawnChildrenAroundAsync(ScatterRuleSO parentRule, Vector3 parentPos,
      CancellationToken ct, int depth = 0) {
      const int MAX_DEPTH = 3;
      if (depth >= MAX_DEPTH || parentRule.childScatters == null) return;

      foreach (var childConfig in parentRule.childScatters) {
        if (childConfig?.rule == null) continue;
        if (ct.IsCancellationRequested) break;

        var childRule = childConfig.rule;
        var count = Random.Range(childConfig.countPerParent.x, childConfig.countPerParent.y + 1);
        var localSpawned = new List<Vector3>();

        for (var i = 0; i < count; i++) {
          if (ct.IsCancellationRequested) break;

          var attempts = 0;
          while (attempts < childRule.maxAttempts) {
            attempts++;

            var angle = Random.Range(0f, Mathf.PI * 2f);
            var radius = Random.Range(childConfig.radiusMin, childConfig.radiusMax);
            var pos = parentPos + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            if (!ValidateTerrainAtRule(childRule, pos)) continue;
            if (childConfig.inheritTerrainFilter && !ValidateTerrainAtRule(parentRule, pos)) continue;

            var spacing = childConfig.localSpacingOnly
              ? ValidateLocalSpacing(childRule.minSpacing, pos, localSpawned)
              : ValidateSpacing(childRule, pos);

            if (!spacing) continue;
            if (!ValidateAvoidance(childRule, pos)) continue;

            if (_actorCreation.TrySpawnActor(childRule.actorKey, pos, out var actor)) {
              actor.transform.SetParent(GetOrCreateContainer(), true);
              if (childRule.randomRotation) {
                actor.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
              }
              actor.transform.localScale = Vector3.one * Random.Range(childRule.scaleRange.x, childRule.scaleRange.y);

              _spawnedActors.Add(actor);
              localSpawned.Add(pos);

              if (childRule.hasChildren) {
                await SpawnChildrenAroundAsync(childRule, pos, ct, depth + 1);
              }
              await YieldIfNeeded(ct);
              break;
            }
          }
        }
      }
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

    private bool TryPlaceActor(BiomeScatterConfig sc, Vector3 position, out ActorDescription actor) {
      actor = null;
      var rule = sc.rule;

      if (!ValidateTerrainAt(sc, position)) return false;
      if (!ValidateSpacing(rule, position)) return false;
      if (!ValidateAvoidance(rule, position)) return false;

      if (!_actorCreation.TrySpawnActor(rule.actorKey, position, out actor)) return false;

      actor.transform.SetParent(GetOrCreateContainer(), true);
      if (rule.randomRotation) {
        actor.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
      }
      actor.transform.localScale = Vector3.one * Random.Range(rule.scaleRange.x, rule.scaleRange.y);

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

    private bool ValidateSpacing(ScatterRuleSO rule, Vector3 position) {
      var sqrSpacing = rule.minSpacing * rule.minSpacing;
      foreach (var actor in _spawnedActors) {
        if (actor != null && (actor.transform.position - position).sqrMagnitude < sqrSpacing)
          return false;
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

    private bool ValidateAvoidance(ScatterRuleSO rule, Vector3 position) {
      if (rule.avoidTags == null || rule.avoidTags.Length == 0) return true;

      var sqrRadius = rule.avoidRadius * rule.avoidRadius;
      foreach (var actor in _spawnedActors) {
        if (actor == null) continue;
        if (!actor.HasAnyTags(rule.avoidTags)) continue;
        if ((actor.transform.position - position).sqrMagnitude < sqrRadius)
          return false;
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

    private Vector3 RandomPointInBounds(Bounds bounds) {
      return new Vector3(
        Random.Range(bounds.min.x, bounds.max.x),
        0,
        Random.Range(bounds.min.z, bounds.max.z)
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
