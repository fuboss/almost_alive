#if UNITY_EDITOR
using Content.Scripts.World;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.PhaseSettings {
  /// <summary>
  /// Settings drawer for Terrain Sculpt phase.
  /// Controls: global noise, slope limiting, rivers, lakes.
  /// </summary>
  public class TerrainSculptSettingsDrawer : IPhaseSettingsDrawer {
    public string PhaseName => "Terrain Sculpt";
    public int PhaseIndex => 1;
    public bool IsFoldedOut { get; set; } = true;

    private bool _globalNoiseFoldout = true;
    private bool _slopeFoldout = true;
    private bool _waterFoldout = true;

    public void Draw(WorldGeneratorConfigSO config, GUIStyle boxStyle) {
      IsFoldedOut = EditorGUILayout.Foldout(IsFoldedOut, $"⚙ {PhaseName} Settings", true);
      if (!IsFoldedOut) return;

      EditorGUILayout.BeginVertical(boxStyle);

      var data = config.Data;
      data.sculptTerrain = EditorGUILayout.Toggle("Sculpt Terrain", data.sculptTerrain);

      if (!data.sculptTerrain) {
        EditorGUILayout.HelpBox("Terrain sculpting is disabled.", MessageType.Info);
        EditorGUILayout.EndVertical();
        return;
      }

      EditorGUILayout.Space(8);

      // ═══════════════════════════════════════════════════════════════
      // GLOBAL NOISE
      // ═══════════════════════════════════════════════════════════════

      _globalNoiseFoldout = EditorGUILayout.Foldout(_globalNoiseFoldout, "🌄 Global Noise", true);
      if (_globalNoiseFoldout) {
        EditorGUI.indentLevel++;
        
        data.useGlobalNoise = EditorGUILayout.Toggle("Enable", data.useGlobalNoise);
        
        if (data.useGlobalNoise) {
          EditorGUILayout.Space(4);
          EditorGUILayout.LabelField("Large Hills", EditorStyles.miniLabel);
          data.globalNoiseAmplitude = EditorGUILayout.Slider("Amplitude (m)", data.globalNoiseAmplitude, 0f, 30f);
          data.globalNoiseScale = EditorGUILayout.Slider("Scale", data.globalNoiseScale, 0.001f, 0.05f);
          
          EditorGUILayout.Space(4);
          EditorGUILayout.LabelField("Fine Detail", EditorStyles.miniLabel);
          data.detailNoiseAmplitude = EditorGUILayout.Slider("Amplitude (m)", data.detailNoiseAmplitude, 0f, 10f);
          data.detailNoiseScale = EditorGUILayout.Slider("Scale", data.detailNoiseScale, 0.01f, 0.2f);
        }
        
        EditorGUI.indentLevel--;
      }

      EditorGUILayout.Space(4);

      // ═══════════════════════════════════════════════════════════════
      // SLOPE CONTROL
      // ═══════════════════════════════════════════════════════════════

      _slopeFoldout = EditorGUILayout.Foldout(_slopeFoldout, "📐 Slope Control", true);
      if (_slopeFoldout) {
        EditorGUI.indentLevel++;
        
        data.limitSlopes = EditorGUILayout.Toggle("Limit Slopes", data.limitSlopes);
        
        if (data.limitSlopes) {
          data.maxSlopeAngle = EditorGUILayout.Slider("Max Angle (°)", data.maxSlopeAngle, 15f, 60f);
          data.slopeSmoothingPasses = EditorGUILayout.IntSlider("Smoothing Passes", data.slopeSmoothingPasses, 0, 5);
          
          EditorGUILayout.HelpBox("NavMesh default max slope is 45°.", MessageType.Info);
        }
        
        EditorGUI.indentLevel--;
      }

      EditorGUILayout.Space(4);

      // ═══════════════════════════════════════════════════════════════
      // WATER (Rivers + Lakes)
      // ═══════════════════════════════════════════════════════════════

      _waterFoldout = EditorGUILayout.Foldout(_waterFoldout, "💧 Water Bodies", true);
      if (_waterFoldout) {
        EditorGUI.indentLevel++;
        
        // Water Level with WaterPlane sync
        EditorGUILayout.BeginHorizontal();
        data.waterLevel = EditorGUILayout.Slider("Water Level (m)", data.waterLevel, 0f, 20f);
        
        var waterPlane = GameObject.Find("WaterPlane");
        if (waterPlane != null) {
          var terrain = Terrain.activeTerrain;
          if (terrain != null) {
            float planeY = waterPlane.transform.position.y - terrain.transform.position.y;
            
            if (Mathf.Abs(planeY - data.waterLevel) > 0.1f) {
              if (GUILayout.Button("↔ Sync", GUILayout.Width(50))) {
                Undo.RecordObject(config, "Sync Water Level");
                data.waterLevel = planeY;
                EditorUtility.SetDirty(config);
              }
            } else {
              GUILayout.Label("✓", GUILayout.Width(50));
            }
          }
        } else {
          if (GUILayout.Button("Create", GUILayout.Width(50))) {
            CreateWaterPlane(data.waterLevel);
          }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(8);
        
        // Rivers section
        EditorGUILayout.LabelField("Rivers", EditorStyles.boldLabel);
        data.generateRivers = EditorGUILayout.Toggle("Generate Rivers", data.generateRivers);
        
        if (data.generateRivers) {
          EditorGUI.indentLevel++;
          data.riverWidth = EditorGUILayout.Slider("Width (m)", data.riverWidth, 2f, 20f);
          data.riverBorderChance = EditorGUILayout.Slider("Border Chance", data.riverBorderChance, 0f, 1f);
          data.riverCenterDepth = EditorGUILayout.Slider("Bed Depth (m)", data.riverCenterDepth, 0.5f, 5f);
          EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(8);
        
        // Lakes info
        EditorGUILayout.LabelField("Lakes", EditorStyles.boldLabel);
        
        // Count water body biomes
        int lakeCount = 0;
        if (config.Data.biomes != null) {
          foreach (var biome in config.Data.biomes) {
            if (biome != null && biome.isWaterBody) lakeCount++;
          }
        }
        
        if (lakeCount > 0) {
          EditorGUILayout.HelpBox(
            $"{lakeCount} water body biome(s) configured.\n" +
            "Lakes are carved below water level automatically.\n" +
            "Configure depth & shore gradient per BiomeSO.", 
            MessageType.Info);
        } else {
          EditorGUILayout.HelpBox(
            "No lake biomes configured.\n" +
            "Enable 'Is Water Body' in BiomeSO to create lakes.", 
            MessageType.Warning);
        }
        
        EditorGUI.indentLevel--;
      }

      EditorGUILayout.EndVertical();
    }

    private void CreateWaterPlane(float waterLevel) {
      var terrain = Terrain.activeTerrain;
      if (terrain == null) {
        Debug.LogError("No terrain found to create WaterPlane");
        return;
      }

      var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
      plane.name = "WaterPlane";
      
      var terrainPos = terrain.transform.position;
      var terrainSize = terrain.terrainData.size;
      
      plane.transform.position = new Vector3(
        terrainPos.x + terrainSize.x * 0.5f,
        terrainPos.y + waterLevel,
        terrainPos.z + terrainSize.z * 0.5f
      );
      
      // Scale to cover terrain
      plane.transform.localScale = new Vector3(terrainSize.x * 0.1f, 1f, terrainSize.z * 0.1f);
      
      // Remove collider
      var collider = plane.GetComponent<Collider>();
      if (collider != null) Object.DestroyImmediate(collider);
      
      // Try to apply water material
      var waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Content/Materials/Water.mat");
      if (waterMat != null) {
        plane.GetComponent<Renderer>().sharedMaterial = waterMat;
      }
      
      Undo.RegisterCreatedObjectUndo(plane, "Create WaterPlane");
      Selection.activeGameObject = plane;
      
      Debug.Log($"[TerrainSculpt] Created WaterPlane at Y={terrainPos.y + waterLevel:F2}");
    }
  }
}
#endif
