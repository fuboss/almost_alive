#if UNITY_EDITOR
using Content.Scripts.World;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.Drawers {
  /// <summary>
  /// Draws debug visualization settings inline.
  /// Allows tweaking gizmo visibility and alpha without leaving Artist Mode.
  /// </summary>
  public class DebugSettingsDrawer : ArtistModeDrawerBase {
    
    private bool _foldout = true;
    
    public DebugSettingsDrawer(ArtistModeState state) : base(state) { }

    public override void Draw() {
      var debugSettings = State.Config?.debugSettings;
      if (debugSettings == null) {
        EditorGUILayout.HelpBox("No DebugSettings assigned to Config", MessageType.Info);
        return;
      }

      EditorGUILayout.BeginVertical(ArtistModeStyles.Box);
      
      // Foldout header
      _foldout = EditorGUILayout.Foldout(_foldout, "🔍 Debug Visualization", true, EditorStyles.foldoutHeader);
      
      if (_foldout) {
        EditorGUI.indentLevel++;
        EditorGUI.BeginChangeCheck();
        
        // Master toggle
        debugSettings.drawBiomeGizmos = EditorGUILayout.Toggle("Draw Biome Gizmos", debugSettings.drawBiomeGizmos);
        
        if (debugSettings.drawBiomeGizmos) {
          EditorGUI.indentLevel++;
          
          // Alpha slider
          debugSettings.gizmoAlpha = EditorGUILayout.Slider("Gizmo Alpha", debugSettings.gizmoAlpha, 0.1f, 1f);
          
          // Label height
          debugSettings.biomeLabelHeight = EditorGUILayout.Slider("Label Height", debugSettings.biomeLabelHeight, 0.5f, 10f);
          
          // Toggles
          debugSettings.drawCellCenters = EditorGUILayout.Toggle("Draw Cell Centers", debugSettings.drawCellCenters);
          debugSettings.drawRiverMarkers = EditorGUILayout.Toggle("Draw River Markers", debugSettings.drawRiverMarkers);
          
          EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(4);
        
        // Logging section
        debugSettings.logGeneration = EditorGUILayout.Toggle("Log Generation", debugSettings.logGeneration);
        
        if (debugSettings.logGeneration) {
          EditorGUI.indentLevel++;
          debugSettings.logDetailedTimings = EditorGUILayout.Toggle("Detailed Timings", debugSettings.logDetailedTimings);
          debugSettings.logWaterSync = EditorGUILayout.Toggle("Water Sync", debugSettings.logWaterSync);
          EditorGUI.indentLevel--;
        }
        
        if (EditorGUI.EndChangeCheck()) {
          EditorUtility.SetDirty(debugSettings);
          SceneView.RepaintAll();
        }
        
        EditorGUI.indentLevel--;
      }
      
      EditorGUILayout.EndVertical();
    }
  }
}
#endif
