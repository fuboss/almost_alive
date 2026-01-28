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
    // TERRAIN SCULPTING
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Terrain Sculpting/Global Noise")]
    [Tooltip("Add global terrain noise independent of biomes")]
    public bool useGlobalNoise = true;

    [FoldoutGroup("Terrain Sculpting/Global Noise")]
    [ShowIf("useGlobalNoise")]
    [Tooltip("Large-scale hills amplitude (meters)")]
    [Range(0f, 30f)]
    public float globalNoiseAmplitude = 10f;

    [FoldoutGroup("Terrain Sculpting/Global Noise")]
    [ShowIf("useGlobalNoise")]
    [Tooltip("Large-scale hills frequency (smaller = bigger hills)")]
    [Range(0.001f, 0.05f)]
    public float globalNoiseScale = 0.008f;

    [FoldoutGroup("Terrain Sculpting/Global Noise")]
    [ShowIf("useGlobalNoise")]
    [Tooltip("Fine detail amplitude (meters)")]
    [Range(0f, 10f)]
    public float detailNoiseAmplitude = 2f;

    [FoldoutGroup("Terrain Sculpting/Global Noise")]
    [ShowIf("useGlobalNoise")]
    [Tooltip("Fine detail frequency")]
    [Range(0.01f, 0.2f)]
    public float detailNoiseScale = 0.05f;

    [FoldoutGroup("Terrain Sculpting/Slope Control")]
    [Tooltip("Limit maximum slope for NavMesh compatibility")]
    public bool limitSlopes = true;

    [FoldoutGroup("Terrain Sculpting/Slope Control")]
    [ShowIf("limitSlopes")]
    [Tooltip("Maximum slope angle (degrees) - NavMesh default is 45")]
    [Range(15f, 60f)]
    public float maxSlopeAngle = 40f;

    [FoldoutGroup("Terrain Sculpting/Slope Control")]
    [ShowIf("limitSlopes")]
    [Tooltip("Smoothing iterations after slope limiting")]
    [Range(0, 5)]
    public int slopeSmoothingPasses = 2;

    [FoldoutGroup("Terrain Sculpting/Rivers")]
    [Tooltip("Generate rivers along biome borders")]
    public bool generateRivers = false;

    [FoldoutGroup("Terrain Sculpting/Rivers")]
    [ShowIf("generateRivers")]
    [Tooltip("River width (meters)")]
    [Range(2f, 20f)]
    public float riverWidth = 6f;

    [FoldoutGroup("Terrain Sculpting/Rivers")]
    [ShowIf("generateRivers")]
    [Tooltip("Probability of river at biome border (0-1)")]
    [Range(0f, 1f)]
    public float riverBorderChance = 0.3f;

    [FoldoutGroup("Terrain Sculpting/Rivers")]
    [ShowIf("generateRivers")]
    [Tooltip("Water surface height (meters) - rivers carved below this")]
    [Range(0f, 20f)]
    public float waterLevel = 5f;

    [FoldoutGroup("Terrain Sculpting/Rivers")]
    [ShowIf("generateRivers")]
    [Tooltip("How much below water level to carve river bed (meters)")]
    [Range(0.5f, 5f)]
    public float riverBedDepth = 1f;

    // ═══════════════════════════════════════════════════════════════
    // DEBUG
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Debug")]
    public bool logGeneration = true;

    [FoldoutGroup("Debug")]
    [Tooltip("Draw biome regions in Scene view")]
    public bool drawBiomeGizmos = true;

    [FoldoutGroup("Debug")]
    [Tooltip("Gizmo grid resolution (higher = more detailed but slower)")]
    [ShowIf("drawBiomeGizmos")]
    [Range(5, 50)]
    public int gizmoResolution = 20;

    [FoldoutGroup("Debug")]
    [Tooltip("Draw Voronoi cell centers")]
    [ShowIf("drawBiomeGizmos")]
    public bool drawCellCenters = true;
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
  }
}
