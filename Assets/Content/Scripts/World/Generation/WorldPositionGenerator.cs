using System.Collections.Generic;
using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World.Generation {
  /// <summary>
  /// Generates spawn positions for world objects.
  /// Supports uniform, clustered, and child-based placement.
  /// </summary>
  public class WorldPositionGenerator {
    private readonly BiomeMap _biomeMap;
    private readonly WorldPlacementValidator _validator;
    private readonly WorldRandom _random;

    public WorldPositionGenerator(BiomeMap biomeMap, WorldPlacementValidator validator, WorldRandom random) {
      _biomeMap = biomeMap;
      _validator = validator;
      _random = random;
    }

    public void GenerateUniformPositions(
      BiomeScatterConfig sc,
      BiomeType biomeType,
      Bounds bounds,
      int targetCount,
      List<(string actorKey, Vector3 position, ScatterRuleSO rule, BiomeType biomeType)> output,
      List<Vector3> spawnedPositions) {
      
      var rule = sc.rule;
      var placed = 0;
      var attempts = 0;
      var maxAttempts = targetCount * rule.maxAttempts;

      while (placed < targetCount && attempts < maxAttempts) {
        attempts++;

        var pos = _random.RandomPointInBounds(bounds);
        if (_biomeMap.GetBiomeAt(pos) != biomeType) continue;
        if (!_validator.ValidatePlacement(sc, pos, spawnedPositions)) continue;

        output.Add((rule.actorKey, pos, rule, biomeType));
        spawnedPositions.Add(pos);
        placed++;

        if (rule.hasChildren) {
          GenerateChildPositions(rule, biomeType, pos, output, spawnedPositions);
        }
      }
    }

    public void GenerateClusteredPositions(
      BiomeScatterConfig sc,
      BiomeType biomeType,
      Bounds bounds,
      int targetCount,
      List<(string actorKey, Vector3 position, ScatterRuleSO rule, BiomeType biomeType)> output,
      List<Vector3> spawnedPositions) {
      
      var rule = sc.rule;
      var remaining = targetCount;
      var clusterAttempts = 0;
      var maxClusterAttempts = targetCount * 10;

      while (remaining > 0 && clusterAttempts < maxClusterAttempts) {
        clusterAttempts++;

        var clusterCenter = _random.RandomPointInBounds(bounds);
        if (_biomeMap.GetBiomeAt(clusterCenter) != biomeType) continue;
        if (!_validator.ValidateTerrainAt(sc, clusterCenter)) continue;

        var clusterCount = Mathf.Min(
          _random.Range(rule.clusterSize.x, rule.clusterSize.y + 1),
          remaining
        );

        var clusterLocalPositions = new List<Vector3>();
        var clusterStartIndex = spawnedPositions.Count;

        for (var i = 0; i < clusterCount; i++) {
          var offset = _random.InsideUnitCircle() * rule.clusterSpread;
          var pos = clusterCenter + new Vector3(offset.x, 0, offset.y);

          if (_biomeMap.GetBiomeAt(pos) != biomeType) continue;
          if (!_validator.ValidateTerrainAt(sc, pos)) continue;
          if (!_validator.ValidateSpacingRange(rule.minSpacing, pos, spawnedPositions, 0, clusterStartIndex)) continue;
          if (!_validator.ValidateLocalSpacing(rule.minSpacing * 0.3f, pos, clusterLocalPositions)) continue;

          output.Add((rule.actorKey, pos, rule, biomeType));
          spawnedPositions.Add(pos);
          clusterLocalPositions.Add(pos);
          remaining--;

          if (rule.hasChildren) {
            GenerateChildPositions(rule, biomeType, pos, output, spawnedPositions);
          }
        }
      }
    }

    public void GenerateChildPositions(
      ScatterRuleSO parentRule,
      BiomeType biomeType,
      Vector3 parentPos,
      List<(string actorKey, Vector3 position, ScatterRuleSO rule, BiomeType biomeType)> output,
      List<Vector3> spawnedPositions,
      int depth = 0) {
      
      const int maxDepth = 3;
      if (depth >= maxDepth || parentRule.childScatters == null) return;

      foreach (var childConfig in parentRule.childScatters) {
        if (childConfig?.rule == null) continue;

        var childRule = childConfig.rule;
        var count = _random.Range(childConfig.countPerParent.x, childConfig.countPerParent.y + 1);
        var localSpawned = new List<Vector3>();

        for (var i = 0; i < count; i++) {
          var attempts = 0;
          while (attempts < childRule.maxAttempts) {
            attempts++;

            var angle = _random.Range(0f, Mathf.PI * 2f);
            var radius = _random.Range(childConfig.radiusMin, childConfig.radiusMax);
            var pos = parentPos + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            if (!_validator.ValidateTerrainAtRule(childRule, pos)) continue;
            if (childConfig.inheritTerrainFilter && !_validator.ValidateTerrainAtRule(parentRule, pos)) continue;

            var spacing = childConfig.localSpacingOnly
              ? _validator.ValidateLocalSpacing(childRule.minSpacing, pos, localSpawned)
              : _validator.ValidateSpacingList(childRule.minSpacing, pos, spawnedPositions);

            if (!spacing) continue;

            output.Add((childRule.actorKey, pos, childRule, biomeType));
            localSpawned.Add(pos);
            spawnedPositions.Add(pos);

            if (childRule.hasChildren) {
              GenerateChildPositions(childRule, biomeType, pos, output, spawnedPositions, depth + 1);
            }
            break;
          }
        }
      }
    }

    public static int CalculateTargetCount(ScatterRuleSO rule, Bounds bounds) {
      if (rule.fixedCount > 0) return rule.fixedCount;
      var area = bounds.size.x * bounds.size.z;
      return Mathf.RoundToInt(area / 100f * rule.density);
    }
  }
}

