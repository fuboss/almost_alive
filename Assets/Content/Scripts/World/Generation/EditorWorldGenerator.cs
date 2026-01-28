#if UNITY_EDITOR
using System.Collections.Generic;
using Content.Scripts.World.Biomes;
using Content.Scripts.World.Vegetation;
using UnityEngine;

namespace Content.Scripts.World.Generation {
  /// <summary>
  /// Synchronous world generation for editor mode.
  /// Generates biomes, terrain, and spawn data.
  /// </summary>
  public static class EditorWorldGenerator {
    /// <summary>
    /// Execute full world generation synchronously.
    /// Populates context.SpawnDataList with actors to spawn.
    /// </summary>
    public static void Generate(EditorGenerationContext context) {
      var config = context.Config;
      var terrain = context.Terrain;
      var seed = context.Seed;
      var bounds = context.GetTerrainBounds();

      // Phase 1: Generate Biomes
      context.UpdateProgress("Creating biome map...", 0.05f);
      context.BiomeMap = VoronoiGenerator.Generate(
        bounds, config.Data.biomes, config.Data.biomeBorderBlend, seed,
        config.Data.minBiomeCells, config.Data.maxBiomeCells
      );

      if (context.BiomeMap == null) {
        Debug.LogError("[EditorWorldGenerator] Failed to generate biome map");
        return;
      }

      config.cachedBiomeMap = context.BiomeMap;

      // Phase 2: Sculpt Terrain
      if (config.Data.sculptTerrain) {
        context.UpdateProgress("Sculpting terrain...", 0.15f);
        context.RegisterTerrainUndo("Sculpt Terrain");
        TerrainSculptor.Sculpt(terrain, context.BiomeMap, seed);
      }

      // Phase 3: Paint Splatmap
      if (config.Data.paintSplatmap) {
        context.UpdateProgress("Painting terrain...", 0.25f);
        context.RegisterTerrainUndo("Paint Splatmap");
        SplatmapPainter.Paint(terrain, context.BiomeMap, seed);
      }

      // Phase 3.5: Generate Feature Map
      context.UpdateProgress("Analyzing terrain features...", 0.28f);
      context.FeatureMap = TerrainFeatureMap.Generate(terrain);
      config.cachedFeatureMap = context.FeatureMap;

      // Phase 3.6: Paint Vegetation
      if (config.Data.paintVegetation) {
        context.UpdateProgress("Painting vegetation...", 0.32f);
        context.RegisterTerrainUndo("Paint Vegetation");
        VegetationPainter.Paint(terrain, context.BiomeMap, config.Data.biomes, seed);
      }

      // Phase 4: Generate positions
      if (config.Data.createScattersInEditor) {
        context.UpdateProgress("Generating positions...", 0.35f);
        GeneratePositions(context, bounds);
      }

      // Phase 5: Generate transforms
      context.UpdateProgress("Generating transforms...", 0.50f);
      GenerateTransforms(context);

      if (config.ShouldLogGeneration) {
        Debug.Log($"[EditorWorldGenerator] Generated {context.SpawnDataList.Count} spawn entries");
      }
    }

    private static void GeneratePositions(EditorGenerationContext context, Bounds bounds) {
      var config = context.Config;
      var positionRandom = new WorldRandom(context.Seed);
      var validator = new WorldPlacementValidator(context.Terrain, context.FeatureMap);
      var positionGenerator = new WorldPositionGenerator(context.BiomeMap, validator, positionRandom);

      var allPositions = new List<(string actorKey, Vector3 position, ScatterRuleSO rule, BiomeType biomeType)>();
      var spawnedPositions = new List<Vector3>();

      foreach (var biome in config.Data.biomes) {
        if (biome?.scatterConfigs == null) continue;

        foreach (var sc in biome.scatterConfigs) {
          if (sc?.rule == null) continue;

          var rule = sc.rule;
          var targetCount = WorldPositionGenerator.CalculateTargetCount(rule, bounds);

          if (config.ShouldLogGeneration) {
            Debug.Log($"[EditorWorldGenerator] {rule.actorName}: target={targetCount}, clustering={rule.useClustering}");
          }

          if (rule.useClustering) {
            positionGenerator.GenerateClusteredPositions(sc, biome.type, bounds, targetCount, allPositions, spawnedPositions);
          } else {
            positionGenerator.GenerateUniformPositions(sc, biome.type, bounds, targetCount, allPositions, spawnedPositions);
          }
        }
      }

      // Store for transform generation
      _tempPositions = allPositions;
    }

    private static List<(string actorKey, Vector3 position, ScatterRuleSO rule, BiomeType biomeType)> _tempPositions;

    private static void GenerateTransforms(EditorGenerationContext context) {
      if (_tempPositions == null) return;

      var transformRandom = new WorldRandom(context.Seed + 1000);
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

        context.SpawnDataList.Add(new WorldSpawnData {
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
#endif

