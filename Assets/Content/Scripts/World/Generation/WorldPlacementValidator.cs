using System;
using System.Collections.Generic;
using Content.Scripts.World.Biomes;
using UnityEngine;

namespace Content.Scripts.World.Generation {
  /// <summary>
  /// Validates placement positions for world objects.
  /// Checks terrain height, slope, layers, spacing, and feature map.
  /// </summary>
  public class WorldPlacementValidator {
    private readonly Terrain _terrain;
    private readonly TerrainFeatureMap _featureMap;

    public WorldPlacementValidator(Terrain terrain, TerrainFeatureMap featureMap = null) {
      _terrain = terrain;
      _featureMap = featureMap;
    }

    public bool ValidatePlacement(BiomeScatterConfig sc, Vector3 pos, List<Vector3> spawned) {
      if (!ValidateTerrainAt(sc, pos)) return false;
      if (!ValidateSpacingList(sc.rule.minSpacing, pos, spawned)) return false;

      if (sc.requiresFeatureMap && _featureMap != null) {
        if (!_featureMap.CheckPlacement(pos, sc.placement)) return false;
      }

      return true;
    }

    public bool ValidateTerrainAt(BiomeScatterConfig sc, Vector3 worldPos) {
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

    public bool ValidateTerrainAtRule(ScatterRuleSO rule, Vector3 worldPos) {
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

    public bool ValidateSpacingList(float minSpacing, Vector3 position, List<Vector3> spawned) {
      var sqrSpacing = minSpacing * minSpacing;
      foreach (var pos in spawned) {
        if ((pos - position).sqrMagnitude < sqrSpacing) return false;
      }
      return true;
    }

    public bool ValidateSpacingRange(float minSpacing, Vector3 position, List<Vector3> spawned, int startIndex, int endIndex) {
      var sqrSpacing = minSpacing * minSpacing;
      for (var i = startIndex; i < endIndex && i < spawned.Count; i++) {
        if ((spawned[i] - position).sqrMagnitude < sqrSpacing) return false;
      }
      return true;
    }

    public bool ValidateLocalSpacing(float minSpacing, Vector3 position, List<Vector3> siblings) {
      var sqrSpacing = minSpacing * minSpacing;
      foreach (var pos in siblings) {
        if ((pos - position).sqrMagnitude < sqrSpacing) return false;
      }
      return true;
    }
  }
}

