using System;
using Content.Scripts.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Trees {
  
  /// <summary>
  /// Configuration data for tree falling physics and effects.
  /// </summary>
  [Serializable]
  public class TreeFallConfig {
    [FoldoutGroup("Physics")]
    [Tooltip("Default tree mass (if not specified in TreeTag)")]
    public float defaultMass = 50f;

    [FoldoutGroup("Physics")]
    [Tooltip("How deep tree is embedded in terrain (collider bottom offset)")]
    public float groundEmbedOffset = 0.5f;

    [FoldoutGroup("Physics")]
    [Tooltip("Initial rotational impulse multiplier")]
    public float initialTorqueMultiplier = 15f;

    [FoldoutGroup("Physics")]
    public float angularDrag = 0.5f;

    [FoldoutGroup("Physics")]
    public float linearDrag = 0.1f;

    [FoldoutGroup("Settled Detection")]
    [Tooltip("Linear velocity threshold for considering tree 'settled' (m/s)")]
    public float settledVelocityThreshold = 0.3f;

    [FoldoutGroup("Settled Detection")]
    [Tooltip("Angular velocity threshold (rad/s)")]
    public float settledAngularThreshold = 0.2f;

    [FoldoutGroup("Settled Detection")]
    [Tooltip("Minimum angle from vertical for considering 'settled' (degrees)")]
    public float settledAngleFromVertical = 60f;

    [FoldoutGroup("Settled Detection")]
    [Tooltip("Delay before checking settled after first impact (sec)")]
    public float settledCheckDelay = 0.5f;

    [FoldoutGroup("Settled Detection")]
    [Tooltip("Maximum fall duration before forced settled (sec)")]
    public float maxFallDuration = 10f;

    [FoldoutGroup("Damage")]
    [Tooltip("Damage multiplier: damage = velocity * mass * multiplier")]
    public float impactDamageMultiplier = 0.5f;

    [FoldoutGroup("Damage")]
    [Tooltip("Minimum velocity for dealing damage (m/s)")]
    public float minVelocityForDamage = 3f;

    [FoldoutGroup("Damage")]
    public LayerMask impactLayers = ~0;

    [FoldoutGroup("Log Generation")]
    [Tooltip("Log diameter to tree width ratio")]
    [Range(0.1f, 0.5f)]
    public float logDiameterRatio = 0.25f;

    [FoldoutGroup("Log Generation")]
    [Tooltip("Number of cross-section sides (6 = hexagon, prevents rolling)")]
    [Range(4, 12)]
    public int logSides = 6;

    [FoldoutGroup("Log Generation")]
    [Tooltip("Actor key for spawning log")]
    public string logActorKey = "log_0";

    [FoldoutGroup("Effects")]
    [Tooltip("Leaf particle system prefab")]
    public GameObject leafBurstPrefab;

    [FoldoutGroup("Effects")]
    [Tooltip("Leaf effect duration (sec)")]
    public float leafBurstDuration = 2f;

    [FoldoutGroup("Direction Strategy")]
    [Tooltip("Probability of falling towards colonist buildings (0-1)")]
    [Range(0f, 1f)]
    public float buildingTargetProbability = 0.3f;

    [FoldoutGroup("Direction Strategy")]
    [Tooltip("Building search radius for directed falling")]
    public float buildingSearchRadius = 20f;
  }

  /// <summary>
  /// ScriptableObject container for TreeFallConfig.
  /// </summary>
  [CreateAssetMenu(fileName = "TreeFallConfig", menuName = "Game/Features/Tree Fall Config")]
  public class TreeFallConfigSO : ScriptableConfig<TreeFallConfig> { }
}
