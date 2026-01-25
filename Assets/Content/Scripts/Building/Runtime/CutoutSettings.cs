using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// SO settings for structure cutout appearance.
  /// </summary>
  [CreateAssetMenu(menuName = "Game/Building/Cutout Settings", fileName = "CutoutSettings")]
  public class CutoutSettings : ScriptableObject {
    public float distance = 15f;
    [Title("Transition")]
    [Tooltip("Speed of fade in/out")]
    [Range(1f, 20f)]
    public float transitionSpeed = 8f;
    
    [Title("Falloff Shape")]
    [Tooltip("Defines edge softness curve (0=center, 1=edge)")]
    public AnimationCurve falloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    
    [Title("Shape")]
    [Tooltip("Optional texture mask for custom cutout shape")]
    public Texture2D shapeMask;
    
    [Tooltip("How to map shape mask")]
    public CutoutShapeMapping shapeMapping = CutoutShapeMapping.Radial;
    
    [Title("Edge")]
    [Tooltip("Edge feathering distance")]
    [Range(0f, 2f)]
    public float edgeSoftness = 0.5f;
  }
  
  public enum CutoutShapeMapping {
    Radial,      // circular from center
    Rectangular, // box-based
    Custom       // use mask texture
  }
}
