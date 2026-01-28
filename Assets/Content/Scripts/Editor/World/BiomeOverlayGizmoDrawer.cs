#if UNITY_EDITOR
using Content.Scripts.World;
using Content.Scripts.World.Biomes;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.World {
  /// <summary>
  /// Draws biome regions as colored overlay in Scene view using Handles.
  /// Replaces the old debug quad system - gizmos can't be occluded by scene objects.
  /// </summary>
  [InitializeOnLoad]
  public static class BiomeOverlayGizmoDrawer {
    
    private static WorldGeneratorConfigSO _config;
    private static Terrain _terrain;
    
    // Grid settings
    private const int GRID_RESOLUTION = 48;
    private const float HEIGHT_OFFSET = 0.15f;

    static BiomeOverlayGizmoDrawer() {
      SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView) {
      if (Event.current.type != EventType.Repaint) return;
      
      if (!EnsureReferences()) return;
      if (!_config.ShouldDrawGizmos) return;
      if (_config.cachedBiomeMap == null) return;

      DrawBiomeOverlay();
    }

    private static bool EnsureReferences() {
      if (_config == null) {
        _config = Resources.Load<WorldGeneratorConfigSO>("Environment/WorldGeneratorConfig");
        if (_config == null) return false;
      }

      if (_terrain == null) {
        _terrain = _config.terrain != null ? _config.terrain : Terrain.activeTerrain;
        if (_terrain == null) return false;
      }

      return true;
    }

    private static void DrawBiomeOverlay() {
      var map = _config.cachedBiomeMap;
      var terrainData = _terrain.terrainData;
      var terrainPos = _terrain.transform.position;
      var terrainSize = terrainData.size;
      
      // Get alpha from debug settings
      var debugSettings = _config.debugSettings;
      var alpha = debugSettings != null ? debugSettings.gizmoAlpha : 0.45f;

      float cellSizeX = terrainSize.x / GRID_RESOLUTION;
      float cellSizeZ = terrainSize.z / GRID_RESOLUTION;

      for (int z = 0; z < GRID_RESOLUTION; z++) {
        for (int x = 0; x < GRID_RESOLUTION; x++) {
          // Cell center in world space
          float worldX = terrainPos.x + (x + 0.5f) * cellSizeX;
          float worldZ = terrainPos.z + (z + 0.5f) * cellSizeZ;
          var samplePos = new Vector3(worldX, 0, worldZ);

          var biome = map.GetBiomeDataAt(samplePos);
          if (biome == null) continue;

          // Sample heights at corners for adaptive terrain following
          var corners = GetCellCorners(x, z, cellSizeX, cellSizeZ, terrainPos);
          
          // Draw filled quad
          var color = biome.debugColor;
          color.a = alpha;
          
          Handles.DrawSolidRectangleWithOutline(corners, color, Color.clear);
        }
      }
    }

    private static Vector3[] GetCellCorners(int x, int z, float cellSizeX, float cellSizeZ, Vector3 terrainPos) {
      var corners = new Vector3[4];
      
      // Bottom-left, bottom-right, top-right, top-left
      float x0 = terrainPos.x + x * cellSizeX;
      float x1 = terrainPos.x + (x + 1) * cellSizeX;
      float z0 = terrainPos.z + z * cellSizeZ;
      float z1 = terrainPos.z + (z + 1) * cellSizeZ;

      corners[0] = new Vector3(x0, SampleHeight(x0, z0) + HEIGHT_OFFSET, z0);
      corners[1] = new Vector3(x1, SampleHeight(x1, z0) + HEIGHT_OFFSET, z0);
      corners[2] = new Vector3(x1, SampleHeight(x1, z1) + HEIGHT_OFFSET, z1);
      corners[3] = new Vector3(x0, SampleHeight(x0, z1) + HEIGHT_OFFSET, z1);

      return corners;
    }

    private static float SampleHeight(float worldX, float worldZ) {
      if (_terrain == null) return 0f;
      return _terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + _terrain.transform.position.y;
    }

    /// <summary>
    /// Force cache clear (call after config/terrain changes).
    /// </summary>
    public static void ClearCache() {
      _config = null;
      _terrain = null;
    }
  }
}
#endif
