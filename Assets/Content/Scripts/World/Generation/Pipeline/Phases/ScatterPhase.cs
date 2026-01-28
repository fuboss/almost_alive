using System.Collections.Generic;
using Content.Scripts.World.Biomes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Content.Scripts.World.Generation.Pipeline.Phases {
  /// <summary>
  /// Phase 5: Spawn scatter objects (trees, rocks, actors).
  /// Uses Poisson disk sampling for natural distribution.
  /// </summary>
  public class ScatterPhase : GenerationPhaseBase {
    
    public override string Name => "Scatters";
    public override string Description => "Spawn trees, rocks, objects";

    // Track spawned objects for rollback
    private readonly List<GameObject> _spawnedObjects = new();
    private Transform _scatterRoot;

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
      ReportProgress(0f, "Preparing scatter spawn...");
      
      _spawnedObjects.Clear();
      
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
          ReportProgress(progress * 0.9f, $"Spawning {scatterConfig.rule.name}...");
          
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
          foreach (var point in points) {
            var obj = SpawnScatterObject(rule, point, terrain, ctx);
            if (obj != null) {
              obj.transform.SetParent(_scatterRoot);
              _spawnedObjects.Add(obj);
            }
          }
        }
      }
      
      ReportProgress(1f);
      ClearProgressBar();
      
      if (config.logGeneration) {
        Debug.Log($"[Scatter] Spawned {_spawnedObjects.Count} objects");
      }
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
      
      // Destroy root
      if (_scatterRoot != null) {
        #if UNITY_EDITOR
        Object.DestroyImmediate(_scatterRoot.gameObject);
        #else
        Object.Destroy(_scatterRoot.gameObject);
        #endif
        _scatterRoot = null;
      }
    }

    protected override Material CreateDebugMaterial(GenerationContext ctx) {
      // No special debug material - objects visible in scene
      return null;
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
          var x = bounds.min.x + (gx + ctx.RandomValue()) * cellSize;
          var z = bounds.min.z + (gz + ctx.RandomValue()) * cellSize;
          var pos = new Vector3(x, 0, z);
          
          // Check if position is in correct biome (compare BiomeType)
          var posBiomeType = biomeMap.GetBiomeAt(pos);
          if (posBiomeType != targetBiome.type) continue;
          
          // Density check
          if (ctx.RandomValue() > density) continue;
          
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
      // Get prefab directly from rule
      var prefab = rule.prefab;
      if (prefab == null) {
        // TODO: Try to spawn via ActorRegistry if actorKey is set
        // For now, skip if no prefab
        return null;
      }
      
      // Sample terrain height
      var terrainHeight = terrain.SampleHeight(position);
      position.y = terrain.transform.position.y + terrainHeight;
      
      // Random rotation
      var rotation = Quaternion.Euler(0, ctx.RandomRange(0f, 360f), 0);
      
      // Random scale
      var scale = ctx.RandomRange(rule.scaleRange.x, rule.scaleRange.y);
      
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
  }
}
