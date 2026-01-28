using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World.Biomes.Data {
  /// <summary>
  /// Texture/splatmap settings for terrain painting.
  /// 4 fixed slots: base, detail, slope, cliff.
  /// </summary>
  [Serializable]
  public class BiomeTextureData {
    
    [TitleGroup("Base Layer")]
    [HideLabel, InlineProperty]
    public TextureSlot baseTexture = new() { layerName = "Grass" };

    [TitleGroup("Detail Layer")]
    [InfoBox("Blended with base layer using noise. Leave empty to disable.")]
    [HideLabel, InlineProperty]
    public DetailTextureSlot detailTexture = new();

    [TitleGroup("Slope Layer")]
    [InfoBox("Applied on slopes between slopeRange.x and slopeRange.y degrees.")]
    [HideLabel, InlineProperty]
    public SlopeTextureSlot slopeTexture = new() { layerName = "Rock", slopeRange = new Vector2(25f, 45f) };

    [TitleGroup("Cliff Layer")]
    [InfoBox("Applied on steep cliffs above threshold.")]
    [HideLabel, InlineProperty]
    public CliffTextureSlot cliffTexture = new() { layerName = "Cliff", threshold = 55f };

    // ═══════════════════════════════════════════════════════════════
    // API
    // ═══════════════════════════════════════════════════════════════

    public int GetBaseLayerIndex() => GetLayerIndex(baseTexture.layerName);
    public int GetDetailLayerIndex() => GetLayerIndex(detailTexture.layerName);
    public int GetSlopeLayerIndex() => GetLayerIndex(slopeTexture.layerName);
    public int GetCliffLayerIndex() => GetLayerIndex(cliffTexture.layerName);

    private int GetLayerIndex(string layerName) {
      var palette = BiomeSO.GetGlobalPalette();
      return palette != null ? palette.GetIndex(layerName) : -1;
    }

    // ═══════════════════════════════════════════════════════════════
    // SLOT CLASSES
    // ═══════════════════════════════════════════════════════════════

    [Serializable]
    public class TextureSlot {
      [HorizontalGroup("Row"), LabelWidth(80)]
      [ValueDropdown("@BiomeSO.GetGlobalPaletteNames()")]
      public string layerName;

      [HorizontalGroup("Row", Width = 50)]
      [ShowInInspector, ReadOnly, PreviewField(45, ObjectFieldAlignment.Right), HideLabel]
      public Texture2D preview => BiomeSO.GetLayerPreview(layerName);
    }

    [Serializable]
    public class DetailTextureSlot {
      [HorizontalGroup("Row"), LabelWidth(80)]
      [ValueDropdown("@BiomeSO.GetGlobalPaletteNames()")]
      public string layerName;

      [HorizontalGroup("Row", Width = 50)]
      [ShowInInspector, ReadOnly, PreviewField(45, ObjectFieldAlignment.Right), HideLabel]
      public Texture2D preview => BiomeSO.GetLayerPreview(layerName);

      [Range(0f, 1f)]
      [Tooltip("How much detail layer blends with base (0 = none, 1 = full)")]
      public float strength = 0.3f;

      [Range(0.001f, 0.1f)]
      [Tooltip("Noise scale for detail blending (smaller = larger patches)")]
      public float noiseScale = 0.02f;

      public bool isEnabled => !string.IsNullOrEmpty(layerName) && strength > 0f;
    }

    [Serializable]
    public class SlopeTextureSlot {
      [HorizontalGroup("Row"), LabelWidth(80)]
      [ValueDropdown("@BiomeSO.GetGlobalPaletteNames()")]
      public string layerName;

      [HorizontalGroup("Row", Width = 50)]
      [ShowInInspector, ReadOnly, PreviewField(45, ObjectFieldAlignment.Right), HideLabel]
      public Texture2D preview => BiomeSO.GetLayerPreview(layerName);

      [MinMaxSlider(0f, 90f, true)]
      [Tooltip("Slope angle range where this texture appears (degrees)")]
      public Vector2 slopeRange = new(25f, 45f);

      public bool isEnabled => !string.IsNullOrEmpty(layerName);
    }

    [Serializable]
    public class CliffTextureSlot {
      [HorizontalGroup("Row"), LabelWidth(80)]
      [ValueDropdown("@BiomeSO.GetGlobalPaletteNames()")]
      public string layerName;

      [HorizontalGroup("Row", Width = 50)]
      [ShowInInspector, ReadOnly, PreviewField(45, ObjectFieldAlignment.Right), HideLabel]
      public Texture2D preview => BiomeSO.GetLayerPreview(layerName);

      [Range(0f, 90f)]
      [Tooltip("Slope angle above which cliff texture is applied")]
      public float threshold = 55f;

      public bool isEnabled => !string.IsNullOrEmpty(layerName);
    }
  }
}
