using System;
using System.Collections.Generic;
using System.Threading;
using Content.Scripts.AI.GOAP;
using Content.Scripts.Game;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Random = UnityEngine.Random;

namespace Content.Scripts.World {
  public class WorldModule : IInitializable, IDisposable {
    private const int SPAWN_MAX_PER_FRAME = 10;
    private const string CONTAINER_NAME = "[World_Generated]";

    private int _spawnedThisFrame;
    private Transform _container;

    [Inject] private readonly ActorCreationModule _actorCreation;
    [Inject] private readonly WorldSaveModule _saveModule;

    private WorldGeneratorConfigSO _config;
    private Terrain _terrain;
    private readonly List<ActorDescription> _spawnedActors = new();
    private CancellationTokenSource _generationCts;

    public IReadOnlyList<ActorDescription> spawnedActors => _spawnedActors;
    public bool isGenerated { get; private set; }
    public bool isGenerating { get; private set; }
    public float generationProgress { get; private set; }

    public event Action<float> OnGenerationProgress;
    public event Action OnGenerationComplete;

    void IInitializable.Initialize() {
      _config = Resources.Load<WorldGeneratorConfigSO>("Environment/WorldGeneratorConfig");
      if (_config == null) {
        Debug.LogWarning("[WorldModule] No WorldGeneratorConfig found in Resources/Environment/");
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
      if (isGenerating) {
        Debug.LogWarning("[WorldModule] Generation already in progress");
        return;
      }

      Clear();
      await UniTask.WaitForSeconds(1f, cancellationToken: externalCt);
      await UniTask.WaitUntil(_actorCreation, c => c.IsInitialized, cancellationToken: externalCt);
      _generationCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
      var ct = _generationCts.Token;

      isGenerating = true;
      generationProgress = 0f;

      var seed = _config.seed != 0 ? _config.seed : Environment.TickCount;
      Random.InitState(seed);
      if (_config.logGeneration) Debug.Log($"[WorldModule] Generating with seed {seed}");

      // Calculate total target count for progress
      var bounds = GetTerrainBounds();
      var totalTarget = 0;
      foreach (var rule in _config.scatterRules) {
        if (rule != null) totalTarget += CalculateTargetCount(rule, bounds);
      }

      var totalPlaced = 0;
      _spawnedThisFrame = 0;

      foreach (var rule in _config.scatterRules) {
        if (rule == null) continue;
        if (ct.IsCancellationRequested) break;

        var targetCount = CalculateTargetCount(rule, bounds);
        var placed = 0;

        if (rule.useClustering) {
          placed = await GenerateClusteredAsync(rule, bounds, targetCount, ct,
            p => UpdateProgress(totalPlaced + p, totalTarget));
        }
        else {
          placed = await GenerateUniformAsync(rule, bounds, targetCount, ct,
            p => UpdateProgress(totalPlaced + p, totalTarget));
        }

        totalPlaced += placed;

        if (_config.logGeneration) {
          Debug.Log($"[WorldModule] {rule.actorKey}: placed {placed}/{targetCount}");
        }
      }

      isGenerating = false;
      isGenerated = !ct.IsCancellationRequested;
      generationProgress = 1f;

      if (isGenerated) {
        if (_config.logGeneration) Debug.Log($"[WorldModule] Generated {_spawnedActors.Count} actors total");
        OnGenerationComplete?.Invoke();
      }

      _generationCts?.Dispose();
      _generationCts = null;
    }

    public void CancelGeneration() {
      _generationCts?.Cancel();
    }

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

      isGenerated = false;
      generationProgress = 0f;
    }

    private Transform GetOrCreateContainer() {
      if (_container != null) return _container;

      var existing = GameObject.Find(CONTAINER_NAME);
      if (existing != null) {
        _container = existing.transform;
      } else {
        _container = new GameObject(CONTAINER_NAME).transform;
      }
      return _container;
    }

    private async UniTask<int> GenerateUniformAsync(ScatterRuleSO rule, Bounds bounds, int targetCount,
      CancellationToken ct, Action<int> onProgress) {
      var placed = 0;
      var attempts = 0;
      var maxTotalAttempts = targetCount * rule.maxAttempts;

      while (placed < targetCount && attempts < maxTotalAttempts) {
        if (ct.IsCancellationRequested) break;

        attempts++;
        var pos = RandomPointInBounds(bounds);
        if (TryPlaceActor(rule, pos, out var actor)) {
          _spawnedActors.Add(actor);
          placed++;
          onProgress?.Invoke(placed);
          await YieldIfNeeded(ct);
        }
      }

      return placed;
    }

    private async UniTask<int> GenerateClusteredAsync(ScatterRuleSO rule, Bounds bounds, int targetCount,
      CancellationToken ct, Action<int> onProgress) {
      var placed = 0;
      var remaining = targetCount;
      var clusterAttempts = 0;
      var maxClusterAttempts = targetCount * 10;

      while (remaining > 0 && clusterAttempts < maxClusterAttempts) {
        if (ct.IsCancellationRequested) break;

        clusterAttempts++;
        var clusterCenter = RandomPointInBounds(bounds);
        if (!ValidateTerrainAt(rule, clusterCenter)) continue;

        var clusterCount = Random.Range(rule.clusterSize.x, rule.clusterSize.y + 1);
        clusterCount = Mathf.Min(clusterCount, remaining);

        for (var i = 0; i < clusterCount; i++) {
          if (ct.IsCancellationRequested) break;

          var offset = Random.insideUnitCircle * rule.clusterSpread;
          var pos = clusterCenter + new Vector3(offset.x, 0, offset.y);

          if (TryPlaceActor(rule, pos, out var actor)) {
            _spawnedActors.Add(actor);
            placed++;
            remaining--;
            onProgress?.Invoke(placed);
            await YieldIfNeeded(ct);
          }
        }
      }

      return placed;
    }

    private async UniTask YieldIfNeeded(CancellationToken ct) {
      _spawnedThisFrame++;
      if (_spawnedThisFrame >= SPAWN_MAX_PER_FRAME) {
        _spawnedThisFrame = 0;
        await UniTask.Yield(PlayerLoopTiming.Update, ct);
      }
    }

    private void UpdateProgress(int current, int total) {
      if (total <= 0) return;
      generationProgress = (float)current / total;
      OnGenerationProgress?.Invoke(generationProgress);
    }

    private bool TryPlaceActor(ScatterRuleSO rule, Vector3 position, out ActorDescription actor) {
      actor = null;

      if (!ValidateTerrainAt(rule, position)) return false;
      if (!ValidateSpacing(rule, position)) return false;
      if (!ValidateAvoidance(rule, position)) return false;

      if (!_actorCreation.TrySpawnActor(rule.actorKey, position, out actor)) return false;

      actor.transform.SetParent(GetOrCreateContainer(), true);

      if (rule.randomRotation) {
        actor.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
      }

      var scale = Random.Range(rule.scaleRange.x, rule.scaleRange.y);
      actor.transform.localScale = Vector3.one * scale;

      return true;
    }

    private bool ValidateTerrainAt(ScatterRuleSO rule, Vector3 worldPos) {
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

    private bool ValidateSpacing(ScatterRuleSO rule, Vector3 position) {
      var sqrSpacing = rule.minSpacing * rule.minSpacing;
      foreach (var actor in _spawnedActors) {
        if (actor == null) continue;
        if ((actor.transform.position - position).sqrMagnitude < sqrSpacing)
          return false;
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
      var margin = _config.edgeMargin;

      return new Bounds(
        pos + size * 0.5f,
        new Vector3(size.x - margin * 2, size.y, size.z - margin * 2)
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