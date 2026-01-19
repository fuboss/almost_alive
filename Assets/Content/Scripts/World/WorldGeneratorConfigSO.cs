using System.Collections.Generic;
using Content.Scripts.World.Biomes;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using Content.Scripts.Editor.World;
#endif

namespace Content.Scripts.World {
  /// <summary>
  /// Main configuration for world generation.
  /// Biomes define terrain appearance and scatter rules.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Generator Config", fileName = "WorldGeneratorConfig")]
  public class WorldGeneratorConfigSO : ScriptableObject {
    
    // ═══════════════════════════════════════════════════════════════
    // TERRAIN
    // ═══════════════════════════════════════════════════════════════

    [BoxGroup("Terrain")]
    [Tooltip("If null, will find Terrain in scene")]
    [SceneObjectsOnly]
    public Terrain terrain;
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

    // ═══════════════════════════════════════════════════════════════
    // GENERATION
    // ═══════════════════════════════════════════════════════════════

    [BoxGroup("Generation")]
    [Tooltip("Random seed (0 = use system time)")]
    public int seed;

    // ═══════════════════════════════════════════════════════════════
    // BIOMES
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Biomes")]
    [Tooltip("Available biome configurations (each biome has its own scatter rules)")]
    [AssetsOnly]
    [ListDrawerSettings(ShowFoldout = true)]
    [Required]
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

    // Cached biome map for gizmo drawing
    [System.NonSerialized] public BiomeMap cachedBiomeMap;

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════════

    [Button("Validate"), FoldoutGroup("Debug")]
    private void Validate() {
      var errors = 0;

      if (biomes == null || biomes.Count == 0) {
        Debug.LogError("[WorldGenConfig] No biomes configured");
        errors++;
      } else {
        foreach (var biome in biomes) {
          if (biome == null) {
            Debug.LogError("[WorldGenConfig] Null biome in list");
            errors++;
            continue;
          }

          if (biome.scatterRules != null) {
            foreach (var rule in biome.scatterRules) {
              if (rule == null) {
                Debug.LogError($"[WorldGenConfig] Null scatter rule in biome {biome.name}");
                errors++;
              } else if (string.IsNullOrEmpty(rule.actorKey) && rule.prefab == null) {
                Debug.LogError($"[WorldGenConfig] Missing actorKey in rule {rule.name} (biome: {biome.name})");
                errors++;
              }
            }
          }
        }
      }

      Debug.Log(errors == 0 ? "✓ Config valid" : $"✗ {errors} errors found");
    }

    public static WorldGeneratorConfigSO GetFromResources() {
      return Resources.Load<WorldGeneratorConfigSO>("Environment/WorldGeneratorConfig");
    }

    // ═══════════════════════════════════════════════════════════════
    // EDITOR BUTTONS
    // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    [FoldoutGroup("Editor")] public bool createScattersInEditor = true;
    [Button(ButtonSizes.Large, Name = "Generate World"), FoldoutGroup("Editor"), GUIColor(0.4f, 0.8f, 0.4f)]
    [PropertyOrder(-10)]
    private void GenerateInEditor() {
      WorldGeneratorEditor.Generate(this);
    }

    [Button(ButtonSizes.Medium, Name = "Clear Generated"), FoldoutGroup("Editor"), GUIColor(1f, 0.6f, 0.6f)]
    [PropertyOrder(-9)]
    private void ClearGenerated() {
      WorldGeneratorEditor.Clear();
      ClearSplatmap();
      ClearBiomePreview();
      FlattenTerrain();
    }

    [Button(ButtonSizes.Medium, Name = "Preview Biomes Only"), FoldoutGroup("Editor"), GUIColor(0.6f, 0.8f, 1f)]
    [PropertyOrder(-8)]
    private void PreviewBiomes() {
      var t = terrain != null ? terrain : Terrain.activeTerrain;
      if (t == null) {
        Debug.LogError("[WorldGenConfig] No terrain found");
        return;
      }

      var bounds = GetTerrainBounds(t);
      var s = seed != 0 ? seed : System.Environment.TickCount;

      cachedBiomeMap = VoronoiGenerator.Generate(bounds, biomes, biomeBorderBlend, s, minBiomeCells, maxBiomeCells);

      UnityEditor.SceneView.RepaintAll();
      Debug.Log($"[WorldGenConfig] Biome preview: {cachedBiomeMap?.cells.Count ?? 0} cells");
    }

    [Button(ButtonSizes.Small, Name = "Clear Preview"), FoldoutGroup("Editor")]
    [PropertyOrder(-7)]
    [ShowIf("@cachedBiomeMap != null")]
    private void ClearBiomePreview() {
      cachedBiomeMap = null;
      UnityEditor.SceneView.RepaintAll();
    }

    [Button(ButtonSizes.Small, Name = "Flatten Terrain"), FoldoutGroup("Editor/Terrain Tools")]
    private void FlattenTerrain() {
      var t = terrain != null ? terrain : Terrain.activeTerrain;
      if (t != null) {
        TerrainSculptor.Flatten(t, 0);
      }
    }

    [Button(ButtonSizes.Small, Name = "Clear Splatmap"), FoldoutGroup("Editor/Terrain Tools")]
    private void ClearSplatmap() {
      var t = terrain != null ? terrain : Terrain.activeTerrain;
      if (t != null) {
        SplatmapPainter.Clear(t, 0);
      }
    }

    public Bounds GetTerrainBounds(Terrain t) {
      var pos = t.transform.position;
      var size = t.terrainData.size;
      return new Bounds(
        pos + size * 0.5f,
        new Vector3(size.x - edgeMargin * 2, size.y, size.z - edgeMargin * 2)
      );
    }
#endif
  }
}
