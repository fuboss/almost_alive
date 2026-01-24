using System;
using System.Collections.Generic;
using System.Threading;
using Content.Scripts.AI.GOAP;
using Content.Scripts.Game;
using Content.Scripts.World.Biomes;
using Content.Scripts.World.Generation;
using Content.Scripts.World.Vegetation;
using Cysharp.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.World {
  /// <summary>
  /// Runtime world generation facade.
  /// Delegates to generation strategies and manages actor spawning.
  /// </summary>
  public class WorldModule : IInitializable, IDisposable {
    private const int SPAWN_MAX_PER_FRAME = 10;
    private const string CONTAINER_NAME = "[World_Generated]";

    [Inject] private readonly ActorCreationModule _actorCreation;
    [Inject] private readonly WorldSaveModule _saveModule;

    private WorldGeneratorConfigSO _config;
    private Terrain _terrain;
    private BiomeMap _biomeMap;
    private TerrainFeatureMap _featureMap;
    private readonly List<ActorDescription> _spawnedActors = new();
    private CancellationTokenSource _generationCts;
    private Transform _container;
    private int _spawnedThisFrame;

    public IReadOnlyList<ActorDescription> spawnedActors => _spawnedActors;
    public BiomeMap biomeMap => _biomeMap;
    public TerrainFeatureMap featureMap => _featureMap;
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
      await UniTask.WaitForSeconds(0.5f, cancellationToken: externalCt);
      SetTerrainFromConfig();
      await UniTask.WaitUntil(_actorCreation, c => c.IsInitialized, cancellationToken: externalCt);

      _generationCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
      var ct = _generationCts.Token;

      isGenerating = true;
      generationProgress = 0f;

      // Select strategy
      var strategy = SelectStrategy();
      
      // Create context
      var context = new WorldGenerationContext(_config, _terrain, _actorCreation, ct) {
        seed = _config.seed != 0 ? _config.seed : Environment.TickCount,
        onProgress = UpdateProgress
      };

      if (_config.logGeneration) {
        Debug.Log($"[WorldModule] Starting generation with seed {context.seed}, strategy: {strategy.GetType().Name}");
      }

      // Execute strategy
      await strategy.ExecuteAsync(context);

      if (ct.IsCancellationRequested) {
        FinishGeneration(false);
        return;
      }

      // Store results from context
      _biomeMap = context.biomeMap;
      _featureMap = context.featureMap;

      // Spawn actors
      await SpawnActorsAsync(context.spawnDataList, ct);

      // Finalize
      FinishGeneration(!ct.IsCancellationRequested);
    }

    private IWorldGenerationStrategy SelectStrategy() {
      var preload = _terrain.GetComponent<DevPreloadWorld>();
      if (preload != null && preload.isPreloaded && preload.spawnDataList.Count > 0) {
        Debug.Log($"[WorldModule] Using PreloadGenerationStrategy ({preload.spawnDataList.Count} actors)");
        return new PreloadGenerationStrategy(preload);
      }

      Debug.Log("[WorldModule] Using FullGenerationStrategy");
      return new FullGenerationStrategy();
    }

    private async UniTask SpawnActorsAsync(List<WorldSpawnData> spawnDataList, CancellationToken ct) {
      UpdateProgress(0.60f);
      _spawnedThisFrame = 0;
      var biomeContainers = new Dictionary<string, Transform>();

      for (var i = 0; i < spawnDataList.Count; i++) {
        if (ct.IsCancellationRequested) break;

        var data = spawnDataList[i];

        // Get or create biome container
        if (!biomeContainers.TryGetValue(data.biomeId, out var biomeContainer)) {
          var biomeGo = new GameObject($"[{data.biomeId}]");
          biomeGo.transform.SetParent(GetOrCreateContainer(), false);
          biomeContainer = biomeGo.transform;
          biomeContainers[data.biomeId] = biomeContainer;
        }

        if (_actorCreation.TrySpawnActorOnGround(data.actorKey, data.position, out var actor)) {
          actor.transform.SetParent(biomeContainer, true);
          actor.transform.rotation = Quaternion.Euler(0, data.rotation, 0);
          actor.transform.localScale = Vector3.one * data.scale;
          _spawnedActors.Add(actor);
        }

        UpdateProgress(0.60f + 0.35f * i / Mathf.Max(1, spawnDataList.Count));
        await YieldIfNeeded(ct);
      }
    }

    private void FinishGeneration(bool success) {
      if (success) {
        var navSurface = _terrain.GetComponent<NavMeshSurface>();
        if (navSurface != null) navSurface.BuildNavMesh();

        if (VegetationManager.Instance != null) {
          VegetationManager.Instance.Initialize(_terrain);
        }

        if (_config.logGeneration) {
          Debug.Log($"[WorldModule] ✓ Generated {_spawnedActors.Count} actors");
        }
      }

      isGenerating = false;
      isGenerated = success;
      generationProgress = success ? 1f : 0f;

      if (success) {
        OnGenerationComplete?.Invoke();
      }

      _generationCts?.Dispose();
      _generationCts = null;
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

      if (_terrain != null) {
        VegetationPainter.Clear(_terrain);
      }

      _biomeMap = null;
      _featureMap = null;
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


    public void Dispose() {
      CancelGeneration();
      _spawnedActors.Clear();
    }
  }
}
