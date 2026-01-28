#if UNITY_EDITOR
using Content.Scripts.World.Generation.Pipeline;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.World {
  /// <summary>
  /// Draws river visualization gizmos in Scene view.
  /// Shows river paths along biome borders with depth indication.
  /// </summary>
  [InitializeOnLoad]
  public static class RiverGizmoDrawer {
    private static GenerationPipeline _pipeline;
    private static WorldGeneratorConfigSO _config;
    private static Terrain _terrain;

    static RiverGizmoDrawer() {
      SceneView.duringSceneGui += OnSceneGUI;
    }

    public static void SetContext(GenerationPipeline pipeline, WorldGeneratorConfigSO config, Terrain terrain) {
      _pipeline = pipeline;
      _config = config;
      _terrain = terrain;
    }

    public static void Clear() {
      _pipeline = null;
      _config = null;
      _terrain = null;
    }

    private static void OnSceneGUI(SceneView sceneView) {
      if (Event.current.type != EventType.Repaint) return;
      if (_pipeline?.Context == null || _terrain == null) return;
      if (_config == null || !_config.Data.generateRivers) return;

      DrawWaterLevelPlane();
      
      // Check debug settings for river markers
      var debugSettings = _config.debugSettings;
      if (debugSettings == null || debugSettings.drawRiverMarkers) {
        DrawRiverGizmos();
      }
    }

    private static void DrawRiverGizmos() {
      var ctx = _pipeline.Context;
      var riverMask = ctx.RiverMask;
      if (riverMask == null) return;

      var terrainData = _terrain.terrainData;
      var terrainPos = _terrain.transform.position;
      var terrainSize = terrainData.size;
      var resolution = riverMask.GetLength(0);
      
      // Get alpha from debug settings
      var debugSettings = _config.debugSettings;
      var alpha = debugSettings != null ? debugSettings.gizmoAlpha : 0.7f;
      
      // Sample every N pixels for performance
      int step = Mathf.Max(1, resolution / 60);
      
      Handles.color = new Color(0.2f, 0.6f, 1f, alpha);
      
      for (int y = 0; y < resolution; y += step) {
        for (int x = 0; x < resolution; x += step) {
          float riverValue = riverMask[y, x];
          if (riverValue < 0.15f) continue;
          
          // Convert to world position
          float nx = x / (float)(resolution - 1);
          float ny = y / (float)(resolution - 1);
          
          float worldX = terrainPos.x + nx * terrainSize.x;
          float worldZ = terrainPos.z + ny * terrainSize.z;
          float worldY = _terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;
          
          var pos = new Vector3(worldX, worldY + 0.3f, worldZ);
          
          // Draw disc at river position - size based on depth
          float discSize = step * terrainSize.x / resolution * riverValue;
          Handles.DrawSolidDisc(pos, Vector3.up, discSize);
        }
      }
    }

    private static void DrawWaterLevelPlane() {
      if (_config == null || _terrain == null) return;
      
      var terrainPos = _terrain.transform.position;
      var terrainSize = _terrain.terrainData.size;
      float waterY = terrainPos.y + _config.Data.waterLevel;
      
      // Get alpha from debug settings
      var debugSettings = _config.debugSettings;
      var alpha = debugSettings != null ? debugSettings.gizmoAlpha : 0.5f;
      var fillAlpha = alpha * 0.3f;
      var outlineAlpha = alpha;
      
      // Draw water level plane as semi-transparent quad
      Handles.color = new Color(0.1f, 0.4f, 0.8f, fillAlpha);
      
      var corners = new Vector3[] {
        new Vector3(terrainPos.x, waterY, terrainPos.z),
        new Vector3(terrainPos.x + terrainSize.x, waterY, terrainPos.z),
        new Vector3(terrainPos.x + terrainSize.x, waterY, terrainPos.z + terrainSize.z),
        new Vector3(terrainPos.x, waterY, terrainPos.z + terrainSize.z)
      };
      
      Handles.DrawSolidRectangleWithOutline(corners, 
        new Color(0.1f, 0.4f, 0.8f, fillAlpha), 
        new Color(0.2f, 0.6f, 1f, outlineAlpha));
      
      // Draw label
      Handles.color = Color.white;
      var labelPos = terrainPos + new Vector3(terrainSize.x * 0.5f, _config.Data.waterLevel + 1f, terrainSize.z * 0.1f);
      Handles.Label(labelPos, $"💧 Water: {_config.Data.waterLevel:F1}m");
    }
  }
}
#endif
