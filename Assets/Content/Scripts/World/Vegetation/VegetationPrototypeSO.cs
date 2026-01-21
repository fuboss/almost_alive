using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Vegetation {
  /// <summary>
  /// Definition of a single vegetation type (grass, flowers, ferns, etc.)
  /// Maps to Unity Terrain DetailPrototype.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Vegetation Prototype", fileName = "VegetationPrototype_")]
  public class VegetationPrototypeSO : ScriptableObject {
    
    [BoxGroup("Rendering")]
    [Tooltip("Texture for billboard grass")]
    [PreviewField(50)]
    public Texture2D texture;
    
    [BoxGroup("Rendering")]
    [Tooltip("Mesh for 3D vegetation (bushes, flowers)")]
    public GameObject prefab;
    
    [BoxGroup("Rendering")]
    [Tooltip("How to render this vegetation")]
    public DetailRenderMode renderMode = DetailRenderMode.GrassBillboard;
    
    [BoxGroup("Rendering")]
    [Tooltip("Use GPU instancing for better performance")]
    public bool useInstancing = true;

    [BoxGroup("Size")]
    [MinMaxSlider(0.1f, 5f, true)]
    public Vector2 widthRange = new(0.5f, 1.5f);
    
    [BoxGroup("Size")]
    [MinMaxSlider(0.1f, 5f, true)]
    public Vector2 heightRange = new(0.5f, 1.5f);

    [BoxGroup("Color")]
    [ColorPalette]
    public Color dryColor = new(0.8f, 0.7f, 0.4f);
    
    [BoxGroup("Color")]
    [ColorPalette]
    public Color healthyColor = new(0.3f, 0.8f, 0.2f);
    
    [BoxGroup("Color")]
    [Tooltip("How much color varies between instances")]
    [Range(0f, 1f)]
    public float noiseSpread = 0.2f;

    /// <summary>
    /// Creates Unity DetailPrototype from this definition.
    /// </summary>
    /// <param name="layerCoverageDensity"></param>
    public DetailPrototype ToDetailPrototype(float layerCoverageDensity) {
      var prototype = new DetailPrototype {
        renderMode = renderMode,
        usePrototypeMesh = prefab != null,
        useInstancing = useInstancing,
        minWidth = widthRange.x,
        maxWidth = widthRange.y,
        minHeight = heightRange.x,
        maxHeight = heightRange.y,
        dryColor = dryColor,
        healthyColor = healthyColor,
        noiseSpread = noiseSpread,
        density = layerCoverageDensity
      };

      if (prefab != null) {
        prototype.prototype = prefab;
      } else if (texture != null) {
        prototype.prototypeTexture = texture;
      }

      return prototype;
    }
  }
}
