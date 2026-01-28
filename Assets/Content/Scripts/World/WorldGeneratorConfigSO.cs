using System;
using System.Collections.Generic;
using Content.Scripts.Utility;
using Content.Scripts.World.Biomes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World {
  
  /// <summary>
  /// Configuration data for world generation.
  /// </summary>
  [Serializable]
  public class WorldGeneratorConfig {
    
    // ═══════════════════════════════════════════════════════════════
    // TERRAIN
    // ═══════════════════════════════════════════════════════════════

    [BoxGroup("Terrain")]
    [Tooltip("Terrain texture palette (applies layers to terrain)")]
    [AssetsOnly]
    public TerrainPaletteSO terrainPalette;

    [BoxGroup("Terrain")]
    public int size = 400;

    [BoxGroup("Terrain")]
    [Tooltip("Margin from terrain edges")]
    [Range(0f, 50f)]
    public float edgeMargin = 10f;

    [BoxGroup("Terrain")]
    [Tooltip("Modify terrain heightmap based on biomes")]
    public bool sculptTerrain = true;

    [BoxGroup("Terrain")]
    [Tooltip("Paint terrain textures based on biomes")]
    public bool paintSplatmap = true;

    [BoxGroup("Terrain")]
    [Tooltip("Paint grass and vegetation based on biomes")]
    public bool paintVegetation = true;

    // ═══════════════════════════════════════════════════════════════
    // GENERATION
    // ═══════════════════════════════════════════════════════════════

    [BoxGroup("Generation")]
    [Tooltip("Random seed (0 = use system time)")]
    public int seed;

    [BoxGroup("Generation")]
    [Tooltip("Create scatter actors in editor mode")]
    public bool createScattersInEditor = true;

    // ═══════════════════════════════════════════════════════════════
    // BIOMES
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Biomes")]
    [Tooltip("Available biome configurations")]
    [AssetsOnly]
    [ListDrawerSettings(ShowFoldout = true)]
    public List<BiomeSO> biomes = new();

    [FoldoutGroup("Biomes")]
    [Tooltip("Width of blend zone between biomes (meters)")]
    [Range(5f, 50f)]
    public float biomeBorderBlend = 15f;

    [FoldoutGroup("Biomes")]
    [Tooltip("Minimum number of biome regions")]
    [Range(4, 50)]
    public int minBiomeCells = 8;

    [FoldoutGroup("Biomes")]
    [Tooltip("Maximum number of biome regions")]
    [Range(4, 100)]
    public int maxBiomeCells = 25;

    [FoldoutGroup("Biomes/Shape Noise")]
    [Tooltip("Apply noise to biome boundaries for organic shapes")]
    public bool useDomainWarping = true;

    [FoldoutGroup("Biomes/Shape Noise")]
    [ShowIf("useDomainWarping")]
    [Tooltip("How much to distort boundaries (meters)")]
    [Range(0f, 50f)]
    public float warpStrength = 20f;

    [FoldoutGroup("Biomes/Shape Noise")]
    [ShowIf("useDomainWarping")]
    [Tooltip("Scale of the warp noise (smaller = larger features)")]
    [Range(0.001f, 0.1f)]
    public float warpScale = 0.02f;

    [FoldoutGroup("Biomes/Shape Noise")]
    [ShowIf("useDomainWarping")]
    [Tooltip("Noise layers for detail")]
    [Range(1, 4)]
    public int warpOctaves = 2;

    // ═══════════════════════════════════════════════════════════════
    // SCULPTING - GLOBAL NOISE
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Sculpting - Global Noise")]
    [Tooltip("Add global terrain noise independent of biomes")]
    public bool useGlobalNoise = true;

    [FoldoutGroup("Sculpting - Global Noise")]
    [ShowIf("useGlobalNoise")]
    [Tooltip("Large-scale hills amplitude (meters)")]
    [Range(0f, 30f)]
    public float globalNoiseAmplitude = 10f;

    [FoldoutGroup("Sculpting - Global Noise")]
    [ShowIf("useGlobalNoise")]
    [Tooltip("Large-scale hills frequency (smaller = bigger hills)")]
    [Range(0.001f, 0.05f)]
    public float globalNoiseScale = 0.008f;

    [FoldoutGroup("Sculpting - Global Noise")]
    [ShowIf("useGlobalNoise")]
    [Tooltip("Fine detail amplitude (meters)")]
    [Range(0f, 10f)]
    public float detailNoiseAmplitude = 2f;

    [FoldoutGroup("Sculpting - Global Noise")]
    [ShowIf("useGlobalNoise")]
    [Tooltip("Fine detail frequency")]
    [Range(0.01f, 0.2f)]
    public float detailNoiseScale = 0.05f;

    // ═══════════════════════════════════════════════════════════════
    // SCULPTING - SLOPE CONTROL
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Sculpting - Slope Control")]
    [Tooltip("Limit maximum slope for NavMesh compatibility")]
    public bool limitSlopes = true;

    [FoldoutGroup("Sculpting - Slope Control")]
    [ShowIf("limitSlopes")]
    [Tooltip("Maximum slope angle (degrees) - NavMesh default is 45")]
    [Range(15f, 60f)]
    public float maxSlopeAngle = 40f;

    [FoldoutGroup("Sculpting - Slope Control")]
    [ShowIf("limitSlopes")]
    [Tooltip("Smoothing iterations after slope limiting")]
    [Range(0, 5)]
    public int slopeSmoothingPasses = 2;

    [FoldoutGroup("Sculpting - Slope Control")]
    [ShowIf("limitSlopes")]
    [Tooltip("Use gentler slope limit near water to preserve shorelines")]
    public bool protectWaterSlopes = true;

    [FoldoutGroup("Sculpting - Slope Control")]
    [ShowIf("@limitSlopes && protectWaterSlopes")]
    [Tooltip("Max slope angle near water (degrees) - more lenient than land")]
    [Range(45f, 90f)]
    public float maxSlopeAngleNearWater = 60f;

    // ═══════════════════════════════════════════════════════════════
    // SCULPTING - RIVERS
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Sculpting - Rivers")]
    [Tooltip("Generate rivers along biome borders")]
    public bool generateRivers = false;

    [FoldoutGroup("Sculpting - Rivers")]
    [ShowIf("generateRivers")]
    [Tooltip("River width (meters)")]
    [Range(2f, 20f)]
    public float riverWidth = 6f;

    [FoldoutGroup("Sculpting - Rivers")]
    [ShowIf("generateRivers")]
    [Tooltip("Probability of river at biome border (0-1)")]
    [Range(0f, 1f)]
    public float riverBorderChance = 0.3f;

    [FoldoutGroup("Sculpting - Rivers")]
    [ShowIf("generateRivers")]
    [Tooltip("Water surface height (meters) - auto-synced from WaterPlane at generation start")]
    [Range(0f, 20f)]
    public float waterLevel = 5f;

    [FoldoutGroup("Sculpting - Rivers")]
    [ShowIf("generateRivers")]
    [Tooltip("River center depth BELOW water surface (meters)")]
    [Range(0.2f, 3f)]
    public float riverCenterDepth = 0.5f;
  }

  /// <summary>
  /// ScriptableObject container for WorldGeneratorConfig.
  /// Also holds runtime-only scene references and cache.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Generator Config", fileName = "WorldGeneratorConfig")]
  public class WorldGeneratorConfigSO : ScriptableConfig<WorldGeneratorConfig> {
    
    // ═══════════════════════════════════════════════════════════════
    // RUNTIME (scene reference, not part of config data)
    // ═══════════════════════════════════════════════════════════════

    [Title("Runtime")]
    [Tooltip("If null, will find Terrain in scene")]
    [SceneObjectsOnly]
    public Terrain terrain;

    [Title("Debug Settings")]
    [Tooltip("Debug and visualization settings (editor-only)")]
    [AssetsOnly]
    public WorldGeneratorDebugSettings debugSettings;

    // ═══════════════════════════════════════════════════════════════
    // RUNTIME CACHE (not serialized)
    // ═══════════════════════════════════════════════════════════════

    [NonSerialized] public BiomeMap cachedBiomeMap;
    [NonSerialized] public TerrainFeatureMap cachedFeatureMap;

    // ═══════════════════════════════════════════════════════════════
    // API
    // ═══════════════════════════════════════════════════════════════

    public static WorldGeneratorConfigSO GetFromResources() {
      return Resources.Load<WorldGeneratorConfigSO>("Environment/WorldGeneratorConfig");
    }

    public Bounds GetTerrainBounds(Terrain t) {
      var pos = t.transform.position;
      var terrainSize = t.terrainData.size;
      return new Bounds(
        pos + terrainSize * 0.5f,
        new Vector3(terrainSize.x - Data.edgeMargin * 2, terrainSize.y, terrainSize.z - Data.edgeMargin * 2)
      );
    }

    // ═══════════════════════════════════════════════════════════════
    // DEBUG HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Quick check if generation logging is enabled</summary>
    public bool ShouldLogGeneration => debugSettings != null && debugSettings.logGeneration;
    
    /// <summary>Quick check if biome gizmos should be drawn</summary>
    public bool ShouldDrawGizmos => debugSettings != null && debugSettings.drawBiomeGizmos;

    // ═══════════════════════════════════════════════════════════════
    // BIOME VALIDATION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Validate all biome configurations against terrain settings.
    /// Checks for height hierarchy issues that could cause cliffs.
    /// </summary>
    public BiomeConfigValidation ValidateBiomes() {
      var result = new BiomeConfigValidation();
      var config = Data;
      
      if (config.biomes == null || config.biomes.Count == 0) {
        return result;
      }
      
      // Validate each biome individually
      foreach (var biome in config.biomes) {
        if (biome == null) continue;
        var biomeResult = biome.ValidateHeights(config.size, config.biomeBorderBlend, config.waterLevel);
        result.Add(biomeResult);
      }
      
      // Validate all possible biome transitions
      for (int i = 0; i < config.biomes.Count; i++) {
        for (int j = i + 1; j < config.biomes.Count; j++) {
          var from = config.biomes[i];
          var to = config.biomes[j];
          if (from == null || to == null) continue;
          
          var transitionResult = BiomeSO.ValidateTransition(from, to, config.biomeBorderBlend);
          result.Add(transitionResult);
        }
      }
      
      return result;
    }

#if UNITY_EDITOR
    [Button("Validate Biome Heights"), PropertyOrder(100)]
    private void ValidateBiomesButton() {
      var result = ValidateBiomes();
      
      Debug.Log($"[BiomeValidation] Checked {result.biomeResults.Count} biomes, {result.transitionResults.Count} transitions");
      
      // Log individual biome results
      foreach (var br in result.biomeResults) {
        foreach (var err in br.errors) {
          Debug.LogError($"[{br.biomeName}] {err}");
        }
        foreach (var warn in br.warnings) {
          Debug.LogWarning($"[{br.biomeName}] {warn}");
        }
      }
      
      // Log transition results
      foreach (var tr in result.transitionResults) {
        foreach (var err in tr.errors) {
          Debug.LogError($"[{tr.fromBiome} ↔ {tr.toBiome}] {err}");
        }
        foreach (var warn in tr.warnings) {
          Debug.LogWarning($"[{tr.fromBiome} ↔ {tr.toBiome}] {warn}");
        }
      }
      
      if (result.isValid && !result.hasWarnings) {
        Debug.Log("[BiomeValidation] ✓ All biome configurations valid!");
      } else {
        Debug.Log($"[BiomeValidation] Found {result.errorCount} errors, {result.warningCount} warnings");
      }
    }
    
    [Button("Show Height Hierarchy"), PropertyOrder(101)]
    private void ShowHeightHierarchy() {
      var config = Data;
      if (config.biomes == null || config.biomes.Count == 0) {
        Debug.Log("[BiomeValidation] No biomes configured");
        return;
      }
      
      // Sort biomes by base height
      var sorted = new System.Collections.Generic.List<BiomeSO>(config.biomes);
      sorted.RemoveAll(b => b == null);
      sorted.Sort((a, b) => a.baseHeight.CompareTo(b.baseHeight));
      
      var sb = new System.Text.StringBuilder();
      sb.AppendLine("=== BIOME HEIGHT HIERARCHY ===");
      sb.AppendLine($"Terrain Size: {config.size}m | Blend: {config.biomeBorderBlend}m | Water: {config.waterLevel}m");
      sb.AppendLine();
      
      BiomeSO prev = null;
      foreach (var biome in sorted) {
        float minH = biome.GetMinHeight();
        float maxH = biome.GetMaxHeight();
        string waterNote = biome.isWaterBody ? " [WATER]" : "";
        
        sb.AppendLine($"{biome.name}{waterNote}");
        sb.AppendLine($"  Base: {biome.baseHeight:F1}m | Range: [{minH:F1} - {maxH:F1}]m");
        
        if (prev != null) {
          float diff = biome.baseHeight - prev.baseHeight;
          float slope = Mathf.Atan2(Mathf.Abs(biome.GetMaxHeight() - prev.GetMinHeight()), config.biomeBorderBlend) * Mathf.Rad2Deg;
          string status = slope > 40f ? "❌ CLIFF" : slope > 25f ? "⚠️ steep" : "✓";
          sb.AppendLine($"  ↑ {diff:F1}m from {prev.name} ({slope:F1}°) {status}");
        }
        sb.AppendLine();
        prev = biome;
      }
      
      Debug.Log(sb.ToString());
    }
#endif
  }
}
