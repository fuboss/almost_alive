using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Debug and visualization settings for world generation.
  /// Editor-only, does not affect actual generation logic.
  /// </summary>
  [CreateAssetMenu(fileName = "WorldGeneratorDebugSettings", menuName = "World/Debug Settings")]
  public class WorldGeneratorDebugSettings : ScriptableObject {
    
    [FoldoutGroup("Scene Gizmos")]
    [Tooltip("Draw biome labels and boundaries in Scene view")]
    public bool drawBiomeGizmos = true;
    
    [FoldoutGroup("Scene Gizmos")]
    [ShowIf("drawBiomeGizmos")]
    [Tooltip("Gizmo transparency (0 = invisible, 1 = opaque)")]
    [Range(0.1f, 1f)]
    public float gizmoAlpha = 0.5f;
    
    [FoldoutGroup("Scene Gizmos")]
    [ShowIf("drawBiomeGizmos")]
    [Tooltip("Height offset of biome labels above terrain surface")]
    [Range(0.5f, 10f)]
    public float biomeLabelHeight = 2f;
    
    [FoldoutGroup("Scene Gizmos")]
    [ShowIf("drawBiomeGizmos")]
    [Tooltip("Draw voronoi cell centers as spheres")]
    public bool drawCellCenters = true;
    
    [FoldoutGroup("Scene Gizmos")]
    [ShowIf("drawBiomeGizmos")]
    [Tooltip("Draw river spawn points as blue circles")]
    public bool drawRiverMarkers = true;
    
    [FoldoutGroup("Console Logging")]
    [Tooltip("Log generation progress to Console")]
    public bool logGeneration = false;
    
    [FoldoutGroup("Console Logging")]
    [ShowIf("logGeneration")]
    [Tooltip("Include detailed phase timings")]
    public bool logDetailedTimings = false;
    
    [FoldoutGroup("Console Logging")]
    [ShowIf("logGeneration")]
    [Tooltip("Log water level synchronization")]
    public bool logWaterSync = true;
  }
}
