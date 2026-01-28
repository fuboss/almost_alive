using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Generation.Noise.Modifiers {
  /// <summary>
  /// Terrace modifier - creates stepped/terraced output.
  /// Great for plateau terrain, rice paddies, stylized landscapes.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Noise/Terrace", fileName = "Noise_Terrace")]
  public class TerraceNoiseSO : NoiseSO {
    
    [BoxGroup("Source")]
    [Required("Noise to terrace")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    public NoiseSO source;

    [BoxGroup("Terrace")]
    [Tooltip("Number of terrace levels")]
    [Range(2, 20)]
    public int levels = 5;

    [BoxGroup("Terrace")]
    [Tooltip("Smoothness of terrace edges (0 = hard steps, 1 = smooth)")]
    [Range(0f, 1f)]
    public float smoothness = 0.3f;

    [BoxGroup("Terrace")]
    [Tooltip("Use custom terrace heights instead of even spacing")]
    public bool useCustomLevels;

    [BoxGroup("Terrace")]
    [ShowIf("useCustomLevels")]
    [Tooltip("Custom height levels (values 0-1)")]
    [ListDrawerSettings(ShowFoldout = false)]
    public float[] customLevels = { 0f, 0.25f, 0.5f, 0.75f, 1f };

    protected override Vector2 NativeRange => new(0f, 1f);

    protected override float SampleRaw(float x, float y, int seed) {
      if (source == null) return 0f;
      
      source.SetSeed(seed);
      var value = source.Sample(x, y);
      
      return ApplyTerrace(value);
    }

    protected override float SampleRaw3D(float x, float y, float z, int seed) {
      if (source == null) return 0f;
      
      source.SetSeed(seed);
      var value = source.Sample(x, y, z);
      
      return ApplyTerrace(value);
    }

    private float ApplyTerrace(float value) {
      value = Mathf.Clamp01(value);
      
      if (useCustomLevels && customLevels != null && customLevels.Length >= 2) {
        return ApplyCustomTerrace(value);
      }
      
      // Even spacing
      var step = 1f / levels;
      var terraced = Mathf.Floor(value / step) * step;
      
      if (smoothness > 0f) {
        // Smooth blend at edges
        var frac = (value % step) / step;
        var blend = Mathf.SmoothStep(0f, 1f, frac);
        var nextLevel = Mathf.Min(terraced + step, 1f);
        terraced = Mathf.Lerp(terraced, Mathf.Lerp(terraced, nextLevel, blend), smoothness);
      }
      
      return terraced;
    }

    private float ApplyCustomTerrace(float value) {
      // Find which level we're between
      for (int i = 0; i < customLevels.Length - 1; i++) {
        var low = customLevels[i];
        var high = customLevels[i + 1];
        
        if (value <= high) {
          var t = (value - low) / (high - low);
          
          if (smoothness > 0f) {
            t = Mathf.SmoothStep(0f, 1f, t);
            return Mathf.Lerp(low, Mathf.Lerp(low, high, t), smoothness);
          }
          
          return low;
        }
      }
      
      return customLevels[^1];
    }

    protected override int GetParameterHash() {
      var customHash = useCustomLevels && customLevels != null ? customLevels.Length : 0;
      return HashCode.Combine(
        base.GetParameterHash(),
        source?.GetHashCode() ?? 0,
        levels, smoothness, useCustomLevels, customHash
      );
    }
  }
}
