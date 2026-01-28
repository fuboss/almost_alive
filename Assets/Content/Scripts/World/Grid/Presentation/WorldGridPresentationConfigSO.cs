using System;
using Content.Scripts.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Grid.Presentation {
  
  /// <summary>
  /// Configuration data for grid visualization (hover, footprint, selection).
  /// </summary>
  [Serializable]
  public class WorldGridPresentationConfig {
    [Title("Grid Settings")]
    [Tooltip("Grid line thickness in world units")]
    [Range(0.01f, 0.5f)]
    public float gridLineThickness = 0.02f;
    
    [Title("Hover Highlight")]
    [Tooltip("Hover color when placement is valid")]
    [ColorUsage(false, true)]
    public Color hoverColorValid = new(0, 1, 0, 0.6f);
    
    [Tooltip("Hover color when placement is invalid")]
    [ColorUsage(false, true)]
    public Color hoverColorInvalid = new(1, 0, 0, 0.6f);
    
    [Tooltip("Pulse animation speed")]
    [Range(0.5f, 5f)]
    public float hoverPulseSpeed = 2f;
    
    [Tooltip("Pulse intensity curve (1.0 = base, 1.2 = peak)")]
    public AnimationCurve hoverPulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.2f);
    
    [Title("Selection Highlight")]
    [Tooltip("Color for selected actor cell highlight")]
    [ColorUsage(false, true)]
    public Color selectionColor = new(0.36f, 0.54f, 0.36f, 0.5f);
    
    [Title("Footprint Preview")]
    [Tooltip("Footprint color when placement is valid")]
    [ColorUsage(false, true)]
    public Color footprintColorValid = new(0, 1, 0, 0.4f);
    
    [Tooltip("Footprint color when placement is invalid")]
    [ColorUsage(false, true)]
    public Color footprintColorInvalid = new(1, 0, 0, 0.4f);
    
    [Title("Performance")]
    [Tooltip("Update hover highlight every N frames (higher = better performance)")]
    [Range(1, 5)]
    public int hoverUpdateInterval = 2;
    
    [Title("Materials")]
    [Tooltip("Material for tile rendering (hover, footprint, slots)")]
    [AssetsOnly, Required]
    public Material tileMaterial;
  }

  /// <summary>
  /// ScriptableObject container for WorldGridPresentationConfig.
  /// </summary>
  [CreateAssetMenu(fileName = "GridPresentationConfig", menuName = "World/Grid Presentation Config")]
  public class WorldGridPresentationConfigSO : ScriptableConfig<WorldGridPresentationConfig> { }
}
