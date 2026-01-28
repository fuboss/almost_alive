#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Content.Scripts.World;
using Content.Scripts.World.Generation.Pipeline;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard {
  /// <summary>
  /// Dockable window for step-by-step world generation (Artist Mode).
  /// Iterate on each phase: tweak settings, regenerate, proceed when satisfied.
  /// </summary>
  public class ArtistModeWindow : OdinEditorWindow {
    private const string CONFIG_PATH = "Assets/Content/Resources/Environment/WorldGeneratorConfig.asset";

    // ═══════════════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════════════

    private GenerationPipeline _pipeline;
    private List<bool> _phaseEnabled = new();
    private int _targetPhaseIndex = -1; // Which phase user wants to stop at

    [HideInInspector] public WorldGeneratorConfigSO config;
    [HideInInspector] public Terrain terrain;
    [HideInInspector] public int seed;
    [HideInInspector] public bool showDebugVisualization = true;

    private Vector2 _scrollPosition;

    // ═══════════════════════════════════════════════════════════════
    // STYLES
    // ═══════════════════════════════════════════════════════════════

    private GUIStyle _headerStyle;
    private GUIStyle _phaseNameStyle;
    private GUIStyle _phaseNameBoldStyle;
    private GUIStyle _statusStyle;
    private GUIStyle _boxStyle;
    private bool _stylesInitialized;

    private void InitStyles() {
      if (_stylesInitialized) return;
      
      _headerStyle = new GUIStyle(EditorStyles.boldLabel) {
        fontSize = 14,
        alignment = TextAnchor.MiddleLeft
      };
      
      _phaseNameStyle = new GUIStyle(EditorStyles.label) {
        alignment = TextAnchor.MiddleLeft
      };
      
      _phaseNameBoldStyle = new GUIStyle(EditorStyles.boldLabel) {
        alignment = TextAnchor.MiddleLeft
      };
      
      _statusStyle = new GUIStyle(EditorStyles.miniLabel) {
        alignment = TextAnchor.MiddleRight,
        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
      };
      
      _boxStyle = new GUIStyle("box") {
        padding = new RectOffset(8, 8, 6, 6),
        margin = new RectOffset(0, 0, 2, 2)
      };
      
      _stylesInitialized = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // MENU
    // ═══════════════════════════════════════════════════════════════

    [MenuItem("AA/Artist Mode Window", priority = 2)]
    public static void OpenWindow() {
      var window = GetWindow<ArtistModeWindow>();
      window.titleContent = new GUIContent("Artist Mode", EditorGUIUtility.IconContent("d_TerrainInspector.TerrainToolSplat").image);
      window.minSize = new Vector2(320, 500);
      window.Show();
    }

    public static void OpenWithConfig(WorldGeneratorConfigSO cfg) {
      var window = GetWindow<ArtistModeWindow>();
      window.config = cfg;
      window.OnConfigChanged();
      window.Show();
    }

    // ═══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    protected override void OnEnable() {
      base.OnEnable();
      
      if (config == null) FindConfig();
      if (terrain == null) FindTerrain();
      SyncSeedFromConfig();
      EnsurePipeline();
      InitPhaseToggles();
    }

    protected override void OnDisable() {
      base.OnDisable();
      UnsubscribePipelineEvents();
    }

    // ═══════════════════════════════════════════════════════════════
    // MAIN GUI
    // ═══════════════════════════════════════════════════════════════

    protected override void OnImGUI() {
      InitStyles();
      
      _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
      
      EditorGUILayout.Space(4);
      
      // Header
      DrawHeader();
      
      EditorGUILayout.Space(8);
      
      // Config & Terrain
      DrawConfigSection();
      
      EditorGUILayout.Space(8);
      
      // Seed
      DrawSeedSection();
      
      EditorGUILayout.Space(12);
      
      // Phases
      DrawPhasesSection();
      
      EditorGUILayout.Space(12);
      
      // Phase-specific settings
      DrawPhaseSettingsSection();
      
      EditorGUILayout.Space(12);
      
      // Actions
      DrawActionsSection();
      
      EditorGUILayout.Space(8);
      
      // Status
      DrawStatusSection();
      
      EditorGUILayout.EndScrollView();
    }

    // ═══════════════════════════════════════════════════════════════
    // HEADER
    // ═══════════════════════════════════════════════════════════════

    private void DrawHeader() {
      EditorGUILayout.BeginHorizontal();
      GUILayout.Label("🎨 Artist Mode", _headerStyle);
      GUILayout.FlexibleSpace();
      
      // Debug viz toggle
      var debugIcon = showDebugVisualization ? "d_scenevis_visible_hover" : "d_scenevis_hidden_hover";
      if (GUILayout.Button(EditorGUIUtility.IconContent(debugIcon, "Toggle Debug Visualization"), 
          GUILayout.Width(28), GUILayout.Height(20))) {
        showDebugVisualization = !showDebugVisualization;
        OnDebugVizChanged();
      }
      
      EditorGUILayout.EndHorizontal();
      
      EditorGUILayout.LabelField("Iterate on each generation phase", EditorStyles.miniLabel);
    }

    // ═══════════════════════════════════════════════════════════════
    // CONFIG SECTION
    // ═══════════════════════════════════════════════════════════════

    private void DrawConfigSection() {
      EditorGUILayout.BeginVertical(_boxStyle);
      
      // Config field
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Config", GUILayout.Width(50));
      EditorGUI.BeginChangeCheck();
      config = (WorldGeneratorConfigSO)EditorGUILayout.ObjectField(config, typeof(WorldGeneratorConfigSO), false);
      if (EditorGUI.EndChangeCheck()) OnConfigChanged();
      if (GUILayout.Button("Find", GUILayout.Width(40))) FindConfig();
      EditorGUILayout.EndHorizontal();
      
      // Terrain field
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Terrain", GUILayout.Width(50));
      terrain = (Terrain)EditorGUILayout.ObjectField(terrain, typeof(Terrain), true);
      if (GUILayout.Button("Find", GUILayout.Width(40))) FindTerrain();
      EditorGUILayout.EndHorizontal();
      
      EditorGUILayout.EndVertical();
    }

    // ═══════════════════════════════════════════════════════════════
    // SEED SECTION
    // ═══════════════════════════════════════════════════════════════

    private void DrawSeedSection() {
      EditorGUILayout.BeginHorizontal();
      
      EditorGUILayout.LabelField("Seed", GUILayout.Width(50));
      
      EditorGUI.BeginChangeCheck();
      seed = EditorGUILayout.IntField(seed);
      if (EditorGUI.EndChangeCheck()) ApplySeedToConfig();
      
      if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh", "Randomize"), GUILayout.Width(28))) {
        seed = UnityEngine.Random.Range(1, int.MaxValue);
        ApplySeedToConfig();
      }
      
      if (GUILayout.Button("0", GUILayout.Width(22))) {
        seed = 0;
        ApplySeedToConfig();
      }
      
      EditorGUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════════════
    // PHASES SECTION
    // ═══════════════════════════════════════════════════════════════

    private void DrawPhasesSection() {
      EditorGUILayout.LabelField("Generation Phases", _headerStyle);
      EditorGUILayout.Space(4);
      
      if (_pipeline == null) {
        EditorGUILayout.HelpBox("Pipeline not initialized", MessageType.Warning);
        return;
      }
      
      for (int i = 0; i < _pipeline.Phases.Count; i++) {
        DrawPhaseRow(i);
      }
    }

    private void DrawPhaseRow(int index) {
      var phase = _pipeline.Phases[index];
      var state = phase.State;
      var isCurrent = _pipeline.CurrentPhaseIndex == index;
      var isTarget = _targetPhaseIndex == index;
      var isCompleted = state == PhaseState.Completed;
      var isPending = state == PhaseState.Pending;
      var canRun = CanRunPhase(index);
      
      // Background color
      Color bgColor;
      if (isCurrent && state == PhaseState.Running) {
        bgColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
      } else if (isCompleted) {
        bgColor = new Color(0.3f, 0.7f, 0.3f, 0.2f);
      } else if (isTarget) {
        bgColor = new Color(0.9f, 0.7f, 0.2f, 0.15f);
      } else {
        bgColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);
      }
      
      var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(32));
      EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 30), bgColor);
      
      GUILayout.Space(4);
      
      // Toggle (enable/disable this as target phase)
      EditorGUI.BeginChangeCheck();
      var wasEnabled = _targetPhaseIndex >= index;
      var isEnabled = GUILayout.Toggle(wasEnabled, "", GUILayout.Width(18));
      if (EditorGUI.EndChangeCheck()) {
        if (isEnabled && !wasEnabled) {
          // Enabling this phase - set as new target
          _targetPhaseIndex = index;
        } else if (!isEnabled && wasEnabled) {
          // Disabling - set target to previous phase
          _targetPhaseIndex = index - 1;
        }
      }
      
      // Status icon
      var (icon, iconColor) = GetPhaseIconAndColor(state, isCurrent);
      var oldColor = GUI.color;
      GUI.color = iconColor;
      GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
      GUI.color = oldColor;
      
      // Phase name
      var nameStyle = isCurrent || isCompleted ? _phaseNameBoldStyle : _phaseNameStyle;
      GUILayout.Label($"{index + 1}. {phase.Name}", nameStyle);
      
      GUILayout.FlexibleSpace();
      
      // Run button (regenerate this phase)
      EditorGUI.BeginDisabledGroup(!canRun);
      var runTooltip = isCompleted ? "Regenerate this phase" : "Run to this phase";
      if (GUILayout.Button(new GUIContent("▶", runTooltip), GUILayout.Width(26), GUILayout.Height(22))) {
        RunPhase(index);
      }
      EditorGUI.EndDisabledGroup();
      
      // Rollback button
      EditorGUI.BeginDisabledGroup(!isCompleted);
      if (GUILayout.Button(new GUIContent("↺", "Rollback this phase"), GUILayout.Width(26), GUILayout.Height(22))) {
        RollbackPhase(index);
      }
      EditorGUI.EndDisabledGroup();
      
      GUILayout.Space(4);
      
      EditorGUILayout.EndHorizontal();
    }

    private (string icon, Color color) GetPhaseIconAndColor(PhaseState state, bool isCurrent) {
      return state switch {
        PhaseState.Pending => ("○", new Color(0.5f, 0.5f, 0.5f)),
        PhaseState.Running => ("●", new Color(0.4f, 0.7f, 1f)),
        PhaseState.Completed => ("✓", new Color(0.4f, 0.9f, 0.4f)),
        PhaseState.Failed => ("✗", new Color(1f, 0.4f, 0.4f)),
        PhaseState.Skipped => ("⊘", new Color(0.8f, 0.8f, 0.4f)),
        _ => ("?", Color.white)
      };
    }

    // ═══════════════════════════════════════════════════════════════
    // PHASE SETTINGS SECTION
    // ═══════════════════════════════════════════════════════════════

    private bool _settingsFoldout = true;

    private void DrawPhaseSettingsSection() {
      if (config == null) return;
      
      // Determine which phase settings to show
      var phaseIndex = _pipeline?.CurrentPhaseIndex ?? _targetPhaseIndex;
      if (phaseIndex < 0) phaseIndex = 0;
      
      var phaseName = _pipeline?.Phases != null && phaseIndex < _pipeline.Phases.Count 
        ? _pipeline.Phases[phaseIndex].Name 
        : "Biome Layout";
      
      _settingsFoldout = EditorGUILayout.Foldout(_settingsFoldout, $"⚙ {phaseName} Settings", true);
      if (!_settingsFoldout) return;
      
      EditorGUILayout.BeginVertical(_boxStyle);
      
      EditorGUI.BeginChangeCheck();
      
      switch (phaseIndex) {
        case 0: DrawBiomeLayoutSettings(); break;
        case 1: DrawTerrainSculptSettings(); break;
        case 2: DrawSplatmapPaintSettings(); break;
        case 3: DrawVegetationSettings(); break;
        case 4: DrawScatterSettings(); break;
      }
      
      if (EditorGUI.EndChangeCheck()) {
        EditorUtility.SetDirty(config);
      }
      
      EditorGUILayout.EndVertical();
    }

    private void DrawBiomeLayoutSettings() {
      var data = config.Data;
      
      EditorGUILayout.LabelField("Cell Count", EditorStyles.boldLabel);
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Min", GUILayout.Width(30));
      data.minBiomeCells = EditorGUILayout.IntSlider(data.minBiomeCells, 4, 50);
      EditorGUILayout.EndHorizontal();
      
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Max", GUILayout.Width(30));
      data.maxBiomeCells = EditorGUILayout.IntSlider(data.maxBiomeCells, data.minBiomeCells, 100);
      EditorGUILayout.EndHorizontal();
      
      EditorGUILayout.Space(8);
      EditorGUILayout.LabelField("Borders", EditorStyles.boldLabel);
      data.biomeBorderBlend = EditorGUILayout.Slider("Blend Width", data.biomeBorderBlend, 5f, 50f);
      
      EditorGUILayout.Space(8);
      EditorGUILayout.LabelField("Shape Noise (Domain Warping)", EditorStyles.boldLabel);
      data.useDomainWarping = EditorGUILayout.Toggle("Enable", data.useDomainWarping);
      
      if (data.useDomainWarping) {
        EditorGUI.indentLevel++;
        data.warpStrength = EditorGUILayout.Slider("Strength", data.warpStrength, 0f, 50f);
        data.warpScale = EditorGUILayout.Slider("Scale", data.warpScale, 0.001f, 0.1f);
        data.warpOctaves = EditorGUILayout.IntSlider("Octaves", data.warpOctaves, 1, 4);
        EditorGUI.indentLevel--;
      }
    }

    private void DrawTerrainSculptSettings() {
      EditorGUILayout.LabelField("Terrain sculpting settings", EditorStyles.miniLabel);
      EditorGUILayout.HelpBox("Edit BiomeSO assets to configure height profiles per biome.", MessageType.Info);
    }

    private void DrawSplatmapPaintSettings() {
      EditorGUILayout.LabelField("Splatmap painting settings", EditorStyles.miniLabel);
      EditorGUILayout.HelpBox("Edit BiomeSO assets to configure textures per biome.", MessageType.Info);
    }

    private void DrawVegetationSettings() {
      EditorGUILayout.LabelField("Vegetation settings", EditorStyles.miniLabel);
      EditorGUILayout.HelpBox("Edit BiomeSO assets to configure vegetation per biome.", MessageType.Info);
    }

    private void DrawScatterSettings() {
      var data = config.Data;
      data.createScattersInEditor = EditorGUILayout.Toggle("Create in Editor", data.createScattersInEditor);
      EditorGUILayout.HelpBox("Edit BiomeSO assets to configure scatter objects per biome.", MessageType.Info);
    }

    // ═══════════════════════════════════════════════════════════════
    // ACTIONS SECTION
    // ═══════════════════════════════════════════════════════════════

    private void DrawActionsSection() {
      EditorGUILayout.BeginVertical(_boxStyle);
      
      EditorGUILayout.BeginHorizontal();
      
      // Run to target
      EditorGUI.BeginDisabledGroup(!CanRunToTarget());
      var targetName = _targetPhaseIndex >= 0 && _targetPhaseIndex < _pipeline.Phases.Count 
        ? _pipeline.Phases[_targetPhaseIndex].Name 
        : "—";
      GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
      if (GUILayout.Button($"▶ Run to: {targetName}", GUILayout.Height(28))) {
        RunToTarget();
      }
      GUI.backgroundColor = Color.white;
      EditorGUI.EndDisabledGroup();
      
      EditorGUILayout.EndHorizontal();
      
      EditorGUILayout.Space(4);
      
      EditorGUILayout.BeginHorizontal();
      
      // Reset
      EditorGUI.BeginDisabledGroup(!CanReset());
      GUI.backgroundColor = new Color(1f, 0.7f, 0.4f);
      if (GUILayout.Button("↺ Reset", GUILayout.Height(24))) {
        ResetPipeline();
      }
      GUI.backgroundColor = Color.white;
      EditorGUI.EndDisabledGroup();
      
      // Clear World
      GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
      if (GUILayout.Button("🗑 Clear", GUILayout.Height(24))) {
        if (EditorUtility.DisplayDialog("Clear World", "Reset terrain and remove all generated objects?", "Clear", "Cancel")) {
          ClearWorld();
        }
      }
      GUI.backgroundColor = Color.white;
      
      EditorGUILayout.EndHorizontal();
      
      EditorGUILayout.EndVertical();
    }

    // ═══════════════════════════════════════════════════════════════
    // STATUS SECTION
    // ═══════════════════════════════════════════════════════════════

    private void DrawStatusSection() {
      var status = GetStatusText();
      EditorGUILayout.LabelField(status, EditorStyles.centeredGreyMiniLabel);
    }

    private string GetStatusText() {
      if (_pipeline == null) return "Pipeline not ready";
      if (!_pipeline.IsRunning) return "Ready to generate";
      if (_pipeline.IsCompleted) return "✓ Generation complete";
      if (_pipeline.IsPaused) {
        var current = _pipeline.CurrentPhase?.Name ?? "—";
        return $"Paused after: {current}";
      }
      return $"Running phase {_pipeline.CurrentPhaseIndex + 1}...";
    }

    // ═══════════════════════════════════════════════════════════════
    // PHASE EXECUTION
    // ═══════════════════════════════════════════════════════════════

    private void RunPhase(int index) {
      Debug.Log($"[ArtistMode] RunPhase({index})");
      
      EnsurePipeline();
      
      if (!_pipeline.IsRunning) {
        Debug.Log($"[ArtistMode] Starting pipeline");
        _pipeline.Begin(config, terrain, artistMode: true);
      }
      
      // If phase already completed, rollback first
      if (index <= _pipeline.CurrentPhaseIndex) {
        Debug.Log($"[ArtistMode] Rolling back to regenerate phase {index}");
        _pipeline.RollbackTo(index - 1);
      }
      
      // Execute phases up to and including target
      while (_pipeline.CurrentPhaseIndex < index && _pipeline.IsRunning) {
        _pipeline.ExecuteNextPhase();
      }
      
      Repaint();
    }

    private void RollbackPhase(int index) {
      if (_pipeline == null || !_pipeline.IsRunning) return;
      _pipeline.RollbackTo(index - 1);
      Repaint();
    }

    private void RunToTarget() {
      if (_targetPhaseIndex < 0) return;
      
      EnsurePipeline();
      
      if (!_pipeline.IsRunning) {
        _pipeline.Begin(config, terrain, artistMode: true);
      }
      
      while (_pipeline.CurrentPhaseIndex < _targetPhaseIndex && _pipeline.IsRunning) {
        _pipeline.ExecuteNextPhase();
      }
      
      Repaint();
    }

    private void ResetPipeline() {
      _pipeline?.Reset();
      Repaint();
    }

    private void ClearWorld() {
      _pipeline?.Reset();
      World.WorldGeneratorEditor.Clear();
      Repaint();
    }

    // ═══════════════════════════════════════════════════════════════
    // CONDITIONS
    // ═══════════════════════════════════════════════════════════════

    private bool CanRunPhase(int index) {
      if (config == null || terrain == null) return false;
      if (_pipeline == null) return true;
      // Can always run if we have config/terrain
      return true;
    }

    private bool CanRunToTarget() {
      if (config == null || terrain == null) return false;
      if (_targetPhaseIndex < 0) return false;
      return true;
    }

    private bool CanReset() {
      return _pipeline != null && _pipeline.IsRunning;
    }

    // ═══════════════════════════════════════════════════════════════
    // PIPELINE MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    private void EnsurePipeline() {
      if (_pipeline == null) {
        _pipeline = new GenerationPipeline();
        SubscribePipelineEvents();
        InitPhaseToggles();
      }
    }

    private void InitPhaseToggles() {
      if (_pipeline == null) return;
      
      _phaseEnabled.Clear();
      for (int i = 0; i < _pipeline.Phases.Count; i++) {
        _phaseEnabled.Add(true);
      }
      
      // Default: target is first phase
      if (_targetPhaseIndex < 0 && _pipeline.Phases.Count > 0) {
        _targetPhaseIndex = 0;
      }
    }

    private void SubscribePipelineEvents() {
      if (_pipeline == null) return;
      _pipeline.OnPhaseCompleted += OnPhaseCompleted;
      _pipeline.OnPipelineCompleted += OnPipelineCompleted;
      _pipeline.OnPipelineReset += OnPipelineReset;
    }

    private void UnsubscribePipelineEvents() {
      if (_pipeline == null) return;
      _pipeline.OnPhaseCompleted -= OnPhaseCompleted;
      _pipeline.OnPipelineCompleted -= OnPipelineCompleted;
      _pipeline.OnPipelineReset -= OnPipelineReset;
    }

    private void OnPhaseCompleted(IGenerationPhase phase) {
      Debug.Log($"[ArtistMode] Phase completed: {phase.Name}");
      
      if (showDebugVisualization && _pipeline?.Context != null) {
        var debugMat = phase.GetDebugMaterial(_pipeline.Context);
        Debug.Log($"[ArtistMode] Debug material: {(debugMat != null ? debugMat.name : "NULL")}");
        _pipeline.Context.SetDebugMaterial(debugMat);
      }
      
      Repaint();
    }

    private void OnPipelineCompleted() {
      _pipeline?.Context?.SetDebugMaterial(null);
      Repaint();
    }

    private void OnPipelineReset() {
      Repaint();
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private void FindConfig() {
      config = AssetDatabase.LoadAssetAtPath<WorldGeneratorConfigSO>(CONFIG_PATH);
      if (config != null) SyncSeedFromConfig();
    }

    private void FindTerrain() {
      terrain = config?.terrain != null ? config.terrain : Terrain.activeTerrain;
    }

    private void OnConfigChanged() {
      SyncSeedFromConfig();
      FindTerrain();
      _pipeline?.Reset();
      _pipeline = null;
      EnsurePipeline();
    }

    private void SyncSeedFromConfig() {
      if (config != null) seed = config.Data.seed;
    }

    private void ApplySeedToConfig() {
      if (config != null && config.Data.seed != seed) {
        Undo.RecordObject(config, "Change Seed");
        config.Data.seed = seed;
        EditorUtility.SetDirty(config);
      }
    }

    private void OnDebugVizChanged() {
      if (_pipeline?.Context == null) return;
      
      if (showDebugVisualization && _pipeline.CurrentPhase != null && 
          _pipeline.CurrentPhase.State == PhaseState.Completed) {
        var debugMat = _pipeline.CurrentPhase.GetDebugMaterial(_pipeline.Context);
        _pipeline.Context.SetDebugMaterial(debugMat);
      } else {
        _pipeline.Context.SetDebugMaterial(null);
      }
    }
  }
}
#endif
