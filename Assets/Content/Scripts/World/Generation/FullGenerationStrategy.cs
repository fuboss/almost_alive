using System.Collections.Generic;
using Content.Scripts.World.Biomes;
using Content.Scripts.World.Vegetation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Content.Scripts.World.Generation {
  /// <summary>
  /// Full world generation strategy.
  /// Generates biomes, sculpts terrain, paints splatmap/vegetation, and creates spawn data.
  /// </summary>
  public class FullGenerationStrategy : IWorldGenerationStrategy {
    public async UniTask ExecuteAsync(WorldGenerationContext context) {
      var config = context.config;
      var terrain = context.terrain;
      var ct = context.cancellationToken;
      var seed = context.seed;

      var bounds = context.GetTerrainBounds();

      // Phase 1: Generate Biomes
      context.onProgress?.Invoke(0.05f);
      context.biomeMap = VoronoiGenerator.Generate(
        bounds, config.biomes, config.biomeBorderBlend, seed,
        config.minBiomeCells, config.maxBiomeCells
      );

      if (context.biomeMap == null) {
        Debug.LogError("[FullGenerationStrategy] Failed to generate biome map");
        return;
      }

      config.cachedBiomeMap = context.biomeMap;

      // Phase 2: Sculpt Terrain
      if (config.sculptTerrain) {
        context.onProgress?.Invoke(0.15f);
        TerrainSculptor.Sculpt(terrain, context.biomeMap, seed);
        await UniTask.Yield(ct);
      }

      // Phase 3: Paint Splatmap
      if (config.paintSplatmap) {
        context.onProgress?.Invoke(0.25f);
        SplatmapPainter.Paint(terrain, context.biomeMap, seed);
        await UniTask.Yield(ct);
      }

      // Phase 3.5: Generate Feature Map
      context.onProgress?.Invoke(0.28f);
      context.featureMap = TerrainFeatureMap.Generate(terrain);
      config.cachedFeatureMap = context.featureMap;

      // Phase 3.6: Paint Vegetation
      if (config.paintVegetation) {
        context.onProgress?.Invoke(0.32f);
        VegetationPainter.Paint(terrain, context.biomeMap, config.biomes, seed);
        await UniTask.Yield(ct);
      }

      // Phase 4: Generate positions
      context.onProgress?.Invoke(0.35f);
      await GeneratePositionsAsync(context, bounds);

      // Phase 5: Generate transforms
      context.onProgress?.Invoke(0.50f);
      GenerateTransforms(context);

      if (config.logGeneration) {
        Debug.Log($"[FullGenerationStrategy] Generated {context.spawnDataList.Count} spawn entries");
      }
    }

    private async UniTask GeneratePositionsAsync(WorldGenerationContext context, Bounds bounds) {
      var config = context.config;
      var ct = context.cancellationToken;

      var positionRandom = new WorldRandom(context.seed);
      var validator = new WorldPlacementValidator(context.terrain, context.featureMap);
      var positionGenerator = new WorldPositionGenerator(context.biomeMap, validator, positionRandom);

      var allPositions = new List<(string actorKey, Vector3 position, ScatterRuleSO rule, BiomeType biomeType)>();
      var spawnedPositions = new List<Vector3>();

      foreach (var biome in config.biomes) {
        if (biome?.scatterConfigs == null) continue;
        if (ct.IsCancellationRequested) break;

        foreach (var sc in biome.scatterConfigs) {
          if (sc?.rule == null) continue;
          if (ct.IsCancellationRequested) break;

          var rule = sc.rule;
          var targetCount = WorldPositionGenerator.CalculateTargetCount(rule, bounds);

          if (config.logGeneration) {
            Debug.Log($"[FullGenerationStrategy] {rule.actorName}: target={targetCount}, clustering={rule.useClustering}");
          }

          if (rule.useClustering) {
            positionGenerator.GenerateClusteredPositions(sc, biome.type, bounds, targetCount, allPositions, spawnedPositions);
          } else {
            positionGenerator.GenerateUniformPositions(sc, biome.type, bounds, targetCount, allPositions, spawnedPositions);
          }
        }
      }

      // Store positions temporarily for transform phase
      _tempPositions = allPositions;
      
      await UniTask.Yield(ct);
    }

    private List<(string actorKey, Vector3 position, ScatterRuleSO rule, BiomeType biomeType)> _tempPositions;

    private void GenerateTransforms(WorldGenerationContext context) {
      if (_tempPositions == null) return;

      var transformRandom = new WorldRandom(context.seed + 1000);
      var biomeCounters = new Dictionary<BiomeType, int>();

      foreach (var (actorKey, position, rule, biomeType) in _tempPositions) {
        var rotation = rule.randomRotation ? transformRandom.Range(0f, 360f) : 0f;
        var scale = transformRandom.Range(rule.scaleRange.x, rule.scaleRange.y);

        // Generate biomeId for hierarchy
        if (!biomeCounters.TryGetValue(biomeType, out var counter)) {
          counter = 0;
        }
        biomeCounters[biomeType] = counter + 1;
        var biomeId = $"Biome_{biomeType}_{counter / 100}";

        context.spawnDataList.Add(new WorldSpawnData {
          actorKey = actorKey,
          position = position,
          rotation = rotation,
          scale = scale,
          biomeId = biomeId
        });
      }

      _tempPositions = null;
    }
  }
}

