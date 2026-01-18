using Content.Scripts.Editor.World;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR

#endif

namespace Content.Scripts.World {
  [CreateAssetMenu(menuName = "World/Scatter Rule", fileName = "ScatterRule_")]
  public class ScatterRuleSO : ScriptableObject {
    [BoxGroup("Actor")]
    [Tooltip("Addressables actor key")]
    public string actorKey;

    [BoxGroup("Actor")]
    [Tooltip("Total instances to spawn (overrides density if > 0)")]
    public int fixedCount;

    [BoxGroup("Distribution")]
    [Tooltip("Instances per 100 square units (ignored if fixedCount > 0)")]
    [Range(0.01f, 10f)]
    public float density = 0.5f;

    [BoxGroup("Distribution")]
    [Tooltip("Minimum distance between instances")]
    [Range(1f, 50f)]
    public float minSpacing = 5f;

    [BoxGroup("Distribution")]
    [Tooltip("Max placement attempts per instance")]
    public int maxAttempts = 30;

    [BoxGroup("Clustering")]
    [Tooltip("Min/max instances per cluster (1,1 = no clustering)")]
    [MinMaxSlider(1, 20, true)]
    public Vector2Int clusterSize = new(1, 1);

    [BoxGroup("Clustering")]
    [Tooltip("Spread radius within cluster")]
    [Range(1f, 30f)]
    public float clusterSpread = 5f;

    [BoxGroup("Terrain Filter")]
    [Tooltip("Allowed slope angle range (degrees)")]
    [MinMaxSlider(0f, 90f, true)]
    public Vector2 slopeRange = new(0f, 30f);

    [BoxGroup("Terrain Filter")]
    [Tooltip("Allowed height range (terrain-relative)")]
    [MinMaxSlider(-100f, 500f, true)]
    public Vector2 heightRange = new(0f, 100f);

    [BoxGroup("Terrain Filter")]
    [Tooltip("Terrain layers allowed for placement")]
    public int[] allowedTerrainLayers;

    [BoxGroup("Avoidance")]
    [Tooltip("Don't spawn near actors with these tags")]
    public string[] avoidTags;

    [BoxGroup("Avoidance")]
    [Tooltip("Minimum distance from avoided actors")]
    [Range(0f, 50f)]
    public float avoidRadius = 5f;

    [BoxGroup("Spawn Variation")]
    [Tooltip("Random Y rotation")]
    public bool randomRotation = true;

    [BoxGroup("Spawn Variation")]
    [Tooltip("Scale variation range")]
    [MinMaxSlider(0.5f, 2f, true)]
    public Vector2 scaleRange = new(0.9f, 1.1f);

    public bool useClustering => clusterSize.x > 1 || clusterSize.y > 1;

    [Button, BoxGroup("Debug")]
    private void EstimateCount() {
      if (fixedCount > 0) {
        Debug.Log($"Fixed count: {fixedCount}");
        return;
      }
      // Assume 500x500 terrain for estimate
      var area = 500f * 500f;
      var estimated = Mathf.RoundToInt(area / 100f * density);
      Debug.Log($"Estimated instances on 500x500 terrain: {estimated}");
    }

#if UNITY_EDITOR
    [Button("Test This Rule"), BoxGroup("Debug"), GUIColor(0.6f, 0.8f, 1f)]
    private void TestThisRule() {
      WorldGeneratorEditor.GenerateSingleRule(this);
    }
#endif
  }
}
