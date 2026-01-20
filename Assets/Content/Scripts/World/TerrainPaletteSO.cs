using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Centralized terrain layer palette.
  /// Single source of truth for all terrain textures.
  /// </summary>
  [CreateAssetMenu(menuName = "World/Terrain Palette", fileName = "TerrainPalette")]
  public class TerrainPaletteSO : ScriptableObject {
    
    [Serializable]
    public class PaletteEntry {
      [HorizontalGroup("Entry", Width = 120)]
      [HideLabel]
      public string name;
      
      [HorizontalGroup("Entry")]
      [HideLabel]
      [AssetsOnly]
      public TerrainLayer layer;
      
      [HorizontalGroup("Entry", Width = 60)]
      [HideLabel]
      [PreviewField(50, ObjectFieldAlignment.Right)]
      [ShowInInspector]
      [ReadOnly]
      public Texture2D preview => layer != null ? layer.diffuseTexture as Texture2D : null;
    }

    [ListDrawerSettings(ShowFoldout = false, DraggableItems = true)]
    [LabelText("Terrain Layers")]
    public List<PaletteEntry> layers = new();

    /// <summary>
    /// Get layer index by name. Returns -1 if not found.
    /// </summary>
    public int GetIndex(string layerName) {
      if (string.IsNullOrEmpty(layerName)) return -1;
      
      for (var i = 0; i < layers.Count; i++) {
        if (layers[i].name == layerName) return i;
      }
      return -1;
    }

    /// <summary>
    /// Get layer index by TerrainLayer reference. Returns -1 if not found.
    /// </summary>
    public int GetIndex(TerrainLayer layer) {
      if (layer == null) return -1;
      
      for (var i = 0; i < layers.Count; i++) {
        if (layers[i].layer == layer) return i;
      }
      return -1;
    }

    /// <summary>
    /// Get all layer names for dropdown.
    /// </summary>
    public string[] GetLayerNames() {
      var names = new string[layers.Count];
      for (var i = 0; i < layers.Count; i++) {
        names[i] = layers[i].name;
      }
      return names;
    }

    /// <summary>
    /// Get TerrainLayer array for applying to Terrain.
    /// </summary>
    public TerrainLayer[] GetTerrainLayers() {
      var result = new TerrainLayer[layers.Count];
      for (var i = 0; i < layers.Count; i++) {
        result[i] = layers[i].layer;
      }
      return result;
    }

    /// <summary>
    /// Apply this palette to terrain (replaces all layers).
    /// </summary>
    public void ApplyToTerrain(Terrain terrain) {
      if (terrain == null) {
        Debug.LogError("[TerrainPalette] No terrain provided");
        return;
      }

      var terrainLayers = GetTerrainLayers();
      terrain.terrainData.terrainLayers = terrainLayers;
      
      Debug.Log($"[TerrainPalette] Applied {terrainLayers.Length} layers to terrain");
    }

    /// <summary>
    /// Validate palette configuration.
    /// </summary>
    public bool Validate(out string error) {
      error = null;

      if (layers == null || layers.Count == 0) {
        error = "No layers in palette";
        return false;
      }

      var names = new HashSet<string>();
      for (var i = 0; i < layers.Count; i++) {
        var entry = layers[i];
        
        if (string.IsNullOrEmpty(entry.name)) {
          if (entry.layer == null) {
            error = $"Layer {i} invalid";
            return false;
          }
          entry.name = entry.layer.name;
        }

        if (entry.layer == null) {
          error = $"Layer '{entry.name}' has no TerrainLayer assigned";
          return false;
        }

        if (!names.Add(entry.name)) {
          error = $"Duplicate layer name: '{entry.name}'";
          return false;
        }
      }

      return true;
    }

#if UNITY_EDITOR
    [Button("Validate"), PropertyOrder(-1)]
    private void ValidateButton() {
      if (Validate(out var error)) {
        Debug.Log("[TerrainPalette] ✓ Palette valid");
      } else {
        Debug.LogError($"[TerrainPalette] ✗ {error}");
      }
    }

    [Button("Apply to Active Terrain"), PropertyOrder(-1), GUIColor(0.4f, 0.8f, 0.4f)]
    private void ApplyToActiveTerrain() {
      var terrain = Terrain.activeTerrain;
      if (terrain == null) {
        Debug.LogError("[TerrainPalette] No active terrain in scene");
        return;
      }

      UnityEditor.Undo.RecordObject(terrain.terrainData, "Apply Terrain Palette");
      ApplyToTerrain(terrain);
    }

    [Button("Import from Active Terrain"), PropertyOrder(-1)]
    private void ImportFromActiveTerrain() {
      var terrain = Terrain.activeTerrain;
      if (terrain == null) {
        Debug.LogError("[TerrainPalette] No active terrain in scene");
        return;
      }

      var terrainLayers = terrain.terrainData.terrainLayers;
      if (terrainLayers == null || terrainLayers.Length == 0) {
        Debug.LogWarning("[TerrainPalette] Terrain has no layers");
        return;
      }

      UnityEditor.Undo.RecordObject(this, "Import Terrain Layers");
      layers.Clear();

      foreach (var layer in terrainLayers) {
        if (layer == null) continue;
        
        layers.Add(new PaletteEntry {
          name = layer.name,
          layer = layer
        });
      }

      UnityEditor.EditorUtility.SetDirty(this);
      Debug.Log($"[TerrainPalette] Imported {layers.Count} layers from terrain");
    }
#endif
  }
}
