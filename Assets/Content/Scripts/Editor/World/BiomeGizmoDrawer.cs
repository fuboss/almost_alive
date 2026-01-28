#if UNITY_EDITOR
using Content.Scripts.World;
using Content.Scripts.World.Biomes;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.World {
  /// <summary>
  /// Draws biome cell center labels in Scene view.
  /// Uses cached BiomeMap from WorldGeneratorConfigSO.
  /// </summary>
  [InitializeOnLoad]
  public static class BiomeGizmoDrawer {
    private static WorldGeneratorConfigSO _config;
    private static Terrain _terrain;
    private static GUIStyle _labelStyle;
    private static GUIStyle _labelStyleShadow;
    private static bool _loggedOnce;

    static BiomeGizmoDrawer() {
      SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView) {
      // Only draw on Repaint event to avoid flickering
      if (Event.current.type != EventType.Repaint) return;
      
      // Find config if not cached
      if (_config == null) {
        _config = Resources.Load<WorldGeneratorConfigSO>("Environment/WorldGeneratorConfig");
        if (_config == null) {
          Debug.LogWarning("[BiomeGizmo] Config not found at Resources/Environment/WorldGeneratorConfig");
          return;
        }
      }

      if (!_config.ShouldDrawGizmos) return;
      
      if (_config.cachedBiomeMap == null) {
        // Don't spam - only log once per second
        return;
      }

      // Find terrain
      if (_terrain == null) {
        _terrain = _config.terrain != null ? _config.terrain : Terrain.activeTerrain;
        if (_terrain == null) {
          Debug.LogWarning("[BiomeGizmo] Terrain not found");
          return;
        }
      }

      DrawCellCenters();
    }

    private static void EnsureStyles() {
      if (_labelStyle != null) return;
      
      _labelStyle = new GUIStyle(EditorStyles.boldLabel) {
        alignment = TextAnchor.MiddleCenter,
        fontSize = 12,
        normal = { textColor = Color.white }
      };
      
      _labelStyleShadow = new GUIStyle(_labelStyle) {
        normal = { textColor = new Color(0, 0, 0, 0.7f) }
      };
    }

    private static void DrawCellCenters() {
      EnsureStyles();
      
      var map = _config.cachedBiomeMap;
      
      if (map.cells == null || map.cells.Count == 0) {
        Debug.LogWarning("[BiomeGizmo] No cells in BiomeMap");
        return;
      }
      
      if (!_loggedOnce) {
        Debug.Log($"[BiomeGizmo] Drawing {map.cells.Count} cell centers");
        _loggedOnce = true;
      }

      var debugSettings = _config.debugSettings;
      var labelHeight = debugSettings != null ? debugSettings.biomeLabelHeight : 2f;
      var drawCenters = debugSettings == null || debugSettings.drawCellCenters;
      var alpha = debugSettings != null ? debugSettings.gizmoAlpha : 0.5f;
      
      foreach (var cell in map.cells) {
        var worldY = _terrain.SampleHeight(new Vector3(cell.center.x, 0, cell.center.y)) 
                   + _terrain.transform.position.y + labelHeight;
        var pos = new Vector3(cell.center.x, worldY, cell.center.y);

        // Get biome color
        var biomeData = map.GetBiomeDataAt(new Vector3(cell.center.x, 0, cell.center.y));
        if (biomeData == null) continue;
        
        var color = biomeData.debugColor;
        var label = biomeData.name;

        // Draw colored disc marker
        if (drawCenters) {
          var discColor = color;
          discColor.a = alpha;
          Handles.color = discColor;
          Handles.DrawSolidDisc(pos, Vector3.up, 2f);
          
          // Draw outline
          Handles.color = new Color(0, 0, 0, alpha);
          Handles.DrawWireDisc(pos, Vector3.up, 2.1f);
        }

        // Draw label with shadow for readability
        var labelPos = pos + Vector3.up * labelHeight;
        Handles.Label(labelPos + new Vector3(0.1f, -0.1f, 0), label, _labelStyleShadow);
        
        _labelStyle.normal.textColor = color;
        Handles.Label(labelPos, label, _labelStyle);
      }
    }

    /// <summary>
    /// Force redraw of gizmos.
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
      _labelStyle = null;
      _labelStyleShadow = null;
      _loggedOnce = false;
    }
  }
}
#endif
