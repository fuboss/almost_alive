#if UNITY_EDITOR
using Content.Scripts.World;
using Content.Scripts.World.Biomes;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.World {
  /// <summary>
  /// Draws biome map visualization in Scene view.
  /// Uses cached BiomeMap from WorldGeneratorConfigSO.
  /// </summary>
  [InitializeOnLoad]
  public static class BiomeGizmoDrawer {
    private static WorldGeneratorConfigSO _config;
    private static Terrain _terrain;

    static BiomeGizmoDrawer() {
      SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView) {
      // Find config if not cached
      if (_config == null) {
        _config = Resources.Load<WorldGeneratorConfigSO>("Environment/WorldGeneratorConfig");
      }

      if (_config == null || !_config.drawBiomeGizmos) return;
      if (_config.cachedBiomeMap == null) return;

      // Find terrain
      if (_terrain == null) {
        _terrain = _config.terrain != null ? _config.terrain : Terrain.activeTerrain;
      }
      if (_terrain == null) return;

      DrawBiomeOverlay();
      
      if (_config.drawCellCenters) {
        DrawCellCenters();
      }
    }

    private static void DrawBiomeOverlay() {
      var map = _config.cachedBiomeMap;
      var bounds = map.bounds;
      var resolution = _config.gizmoResolution;

      var stepX = bounds.size.x / resolution;
      var stepZ = bounds.size.z / resolution;
      var halfStep = new Vector3(stepX * 0.5f, 0, stepZ * 0.5f);

      Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

      for (var x = 0; x < resolution; x++) {
        for (var z = 0; z < resolution; z++) {
          var worldX = bounds.min.x + x * stepX + halfStep.x;
          var worldZ = bounds.min.z + z * stepZ + halfStep.z;
          var pos2D = new Vector2(worldX, worldZ);

          var query = map.QueryBiome(pos2D);
          if (query.primaryData == null) continue;

          // Sample terrain height
          var worldY = _terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + _terrain.transform.position.y + 0.5f;
          var center = new Vector3(worldX, worldY, worldZ);

          // Blend colors if on border
          Color color;
          if (query.isBlending && query.secondaryData != null) {
            color = Color.Lerp(query.secondaryData.debugColor, query.primaryData.debugColor, query.primaryWeight);
          } else {
            color = query.primaryData.debugColor;
          }

          color.a = 0.4f;
          Handles.color = color;

          // Draw quad
          var size = new Vector3(stepX * 0.95f, 0.1f, stepZ * 0.95f);
          Handles.DrawSolidDisc(center, Vector3.up, Mathf.Min(stepX, stepZ) * 0.4f);
        }
      }
    }

    private static void DrawCellCenters() {
      var map = _config.cachedBiomeMap;

      foreach (var cell in map.cells) {
        var worldY = _terrain.SampleHeight(new Vector3(cell.center.x, 0, cell.center.y)) + _terrain.transform.position.y + 2f;
        var pos = new Vector3(cell.center.x, worldY, cell.center.y);

        // Get biome color
        var biomeData = map.GetBiomeDataAt(new Vector3(cell.center.x, 0, cell.center.y));
        var color = biomeData != null ? biomeData.debugColor : Color.white;

        // Draw marker
        Handles.color = color;
        Handles.DrawSolidDisc(pos, Vector3.up, 3f);

        // Draw label
        var style = new GUIStyle(GUI.skin.label) {
          alignment = TextAnchor.MiddleCenter,
          fontStyle = FontStyle.Bold,
          normal = { textColor = Color.white }
        };
        Handles.Label(pos + Vector3.up * 5f, cell.type.ToString(), style);
      }
    }

    /// <summary>
    /// Force redraw of gizmos (call after biome map changes).
    /// </summary>
    public static void Repaint() {
      SceneView.RepaintAll();
    }

    /// <summary>
    /// Clear cached references.
    /// </summary>
    public static void ClearCache() {
      _config = null;
      _terrain = null;
    }
  }
}
#endif
