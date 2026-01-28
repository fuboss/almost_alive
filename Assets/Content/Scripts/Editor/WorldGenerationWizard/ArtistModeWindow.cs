#if UNITY_EDITOR
using System.Collections.Generic;
using Content.Scripts.Editor.WorldGenerationWizard.ArtistMode;
using Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.Drawers;
using Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.PhaseSettings;
using Content.Scripts.World;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard {
  /// <summary>
  /// Dockable window for step-by-step world generation (Artist Mode).
  /// Iterate on each phase: tweak settings, regenerate, proceed when satisfied.
  /// 
  /// This is a slim coordinator — actual drawing is delegated to Drawers.
  /// </summary>
  public class ArtistModeWindow : OdinEditorWindow {
    
    private ArtistModeState _state;
    private List<ArtistModeDrawerBase> _drawers;
    private Dictionary<int, IPhaseSettingsDrawer> _phaseSettingsDrawers;
    private Vector2 _scrollPosition;

    // ═══════════════════════════════════════════════════════════════
    // MENU
    // ═══════════════════════════════════════════════════════════════

    [MenuItem("AA/Artist Mode Window", priority = 2)]
    public static void OpenWindow() {
      var window = GetWindow<ArtistModeWindow>();
      window.titleContent = new GUIContent("Artist Mode",
        EditorGUIUtility.IconContent("d_TerrainInspector.TerrainToolSplat").image);
      window.minSize = new Vector2(320, 500);
      window.Show();
    }

    public static void OpenWithConfig(WorldGeneratorConfigSO cfg) {
      var window = GetWindow<ArtistModeWindow>();
      window._state.Config = cfg;
      window._state.OnConfigChanged();
      window.Show();
    }

    // ═══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    protected override void OnEnable() {
      base.OnEnable();

      _state = new ArtistModeState();
      _state.LoadDefaults();
      _state.OnStateChanged += Repaint;

      InitDrawers();
      InitPhaseSettingsDrawers();
    }

    protected override void OnDisable() {
      base.OnDisable();
      
      if (_state != null) {
        _state.OnStateChanged -= Repaint;
        _state.Cleanup();
      }
    }

    private void InitDrawers() {
      _drawers = new List<ArtistModeDrawerBase> {
        new HeaderDrawer(_state),
        new ConfigDrawer(_state),
        new SeedDrawer(_state),
        new PhasesListDrawer(_state),
        new ActionsDrawer(_state),
        new StatusDrawer(_state),
        new DebugSettingsDrawer(_state)
      };
    }

    private void InitPhaseSettingsDrawers() {
      _phaseSettingsDrawers = new Dictionary<int, IPhaseSettingsDrawer> {
        [0] = new BiomeLayoutSettingsDrawer(),
        [1] = new TerrainSculptSettingsDrawer(),
        [2] = new SplatmapPaintSettingsDrawer(),
        [3] = new VegetationSettingsDrawer(),
        [4] = new ScatterSettingsDrawer()
      };
    }

    // ═══════════════════════════════════════════════════════════════
    // GUI
    // ═══════════════════════════════════════════════════════════════

    protected override void OnImGUI() {
      ArtistModeStyles.Initialize();

      _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

      EditorGUILayout.Space(4);

      // Draw main sections
      for (int i = 0; i < _drawers.Count; i++) {
        _drawers[i].Draw();
        
        // Insert phase settings after PhasesListDrawer (index 3)
        if (i == 3) {
          EditorGUILayout.Space(12);
          DrawPhaseSettings();
        }
        
        EditorGUILayout.Space(8);
      }

      EditorGUILayout.EndScrollView();
    }

    private void DrawPhaseSettings() {
      if (_state.Config == null) return;

      var phaseIndex = _state.GetCurrentPhaseIndex();
      
      if (_phaseSettingsDrawers.TryGetValue(phaseIndex, out var drawer)) {
        EditorGUI.BeginChangeCheck();
        drawer.Draw(_state.Config, ArtistModeStyles.Box);
        if (EditorGUI.EndChangeCheck()) {
          EditorUtility.SetDirty(_state.Config);
        }
      }
    }
  }
}
#endif
