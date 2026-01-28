using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Configuration for spawning actors in the world.
  /// Assigned to biomes via BiomeSO.scatterRules.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Scatter Rule", fileName = "ScatterRule_")]
  public class ScatterRuleSO : ScriptableObject {
    // ═══════════════════════════════════════════════════════════════
    // ACTOR
    // ═══════════════════════════════════════════════════════════════

    [BoxGroup("Actor")] [Tooltip("Addressables actor key")]
    public string actorKey;
    
    [BoxGroup("Actor")] [Tooltip("Total instances to spawn per biome (overrides density if > 0)")]
    public int fixedCount;

    // ═══════════════════════════════════════════════════════════════
    // DISTRIBUTION
    // ═══════════════════════════════════════════════════════════════

    [BoxGroup("Distribution")]
    [Tooltip("Instances per 100 square units (ignored if fixedCount > 0)")]
    [Range(0.01f, 10f)]
    public float density = 0.5f;

    [BoxGroup("Distribution")] [Tooltip("Minimum distance between instances")] [Range(1f, 50f)]
    public float minSpacing = 5f;

    [BoxGroup("Distribution")] [Tooltip("Max placement attempts per instance")]
    public int maxAttempts = 30;

    // ═══════════════════════════════════════════════════════════════
    // CLUSTERING
    // ═══════════════════════════════════════════════════════════════

    [BoxGroup("Clustering")]
    [Tooltip("Min/max instances per cluster (1,1 = no clustering)")]
    [MinMaxSlider(1, 20, true)]
    public Vector2Int clusterSize = new(1, 1);

    [BoxGroup("Clustering")] [Tooltip("Spread radius within cluster")] [Range(1f, 30f)]
    public float clusterSpread = 5f;

    // ═══════════════════════════════════════════════════════════════
    // TERRAIN FILTER
    // ═══════════════════════════════════════════════════════════════

    [BoxGroup("Terrain Filter")] [Tooltip("Allowed slope angle range (degrees)")] [MinMaxSlider(0f, 90f, true)]
    public Vector2 slopeRange = new(0f, 30f);

    [BoxGroup("Terrain Filter")] [Tooltip("Allowed height range (terrain-relative)")] [MinMaxSlider(-100f, 500f, true)]
    public Vector2 heightRange = new(0f, 100f);

    [BoxGroup("Terrain Filter")] [Tooltip("Terrain layers allowed for placement (empty = all)")]
    public int[] allowedTerrainLayers;

    // ═══════════════════════════════════════════════════════════════
    // AVOIDANCE
    // ═══════════════════════════════════════════════════════════════

    [BoxGroup("Avoidance")] [Tooltip("Don't spawn near actors with these tags")]
    public string[] avoidTags;

    [BoxGroup("Avoidance")] [Tooltip("Minimum distance from avoided actors")] [Range(0f, 50f)]
    public float avoidRadius = 5f;

    // ═══════════════════════════════════════════════════════════════
    // SPAWN VARIATION
    // ═══════════════════════════════════════════════════════════════

    [BoxGroup("Spawn Variation")] [Tooltip("Random Y rotation")]
    public bool randomRotation = true;

    [BoxGroup("Spawn Variation")] [Tooltip("Scale variation range")] [MinMaxSlider(0.5f, 2f, true)]
    public Vector2 scaleRange = new(0.9f, 1.1f);

    // ═══════════════════════════════════════════════════════════════
    // CHILD SCATTERS
    // ═══════════════════════════════════════════════════════════════

    [FoldoutGroup("Child Scatters")]
    [Tooltip("Actors to spawn around each instance of this rule")]
    [ListDrawerSettings(ShowFoldout = true)]
    public List<ChildScatterConfig> childScatters = new();

    // ═══════════════════════════════════════════════════════════════
    // PROPERTIES
    // ═══════════════════════════════════════════════════════════════

    public bool useClustering => clusterSize.x > 1 || clusterSize.y > 1;
    public bool hasChildren => childScatters != null && childScatters.Count > 0;

    public string actorName =>
      !string.IsNullOrWhiteSpace(actorKey)
        ? actorKey
        :"NULL";
  }
}