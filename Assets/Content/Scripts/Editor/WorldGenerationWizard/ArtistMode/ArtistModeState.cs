#if UNITY_EDITOR
using System;
using Content.Scripts.Editor.World;
using Content.Scripts.World;
using Content.Scripts.World.Generation.Pipeline;
using UnityEditor;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode {
  /// <summary>
  /// State container and pipeline manager for Artist Mode.
  /// Separates state/logic from GUI drawing.
  /// Debug visualization now handled by Gizmo drawers (BiomeOverlayGizmoDrawer, RiverGizmoDrawer).
  /// </summary>
  public class ArtistModeState {
    private const string CONFIG_PATH = "Assets/Content/Resources/Environment/WorldGeneratorConfig.asset";

    // ═══════════════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════════════

    public GenerationPipeline Pipeline { get; private set; }
    public WorldGeneratorConfigSO Config { get; set; }
    public Terrain Terrain { get; set; }
    public int Seed { get; set; }
    public int TargetPhaseIndex { get; set; } = -1;

    // ═══════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════

    public event Action OnStateChanged;
    public event Action<IGenerationPhase> OnPhaseCompleted;

    // ═══════════════════════════════════════════════════════════════
    // INITIALIZATION
    // ═══════════════════════════════════════════════════════════════

    public void LoadDefaults() {
      if (Config == null) FindConfig();
      if (Terrain == null) FindTerrain();
      SyncSeedFromConfig();
      EnsurePipeline();
      InitTargetPhase();
    }

    public void Cleanup() {
      UnsubscribePipelineEvents();
    }

    // ═══════════════════════════════════════════════════════════════
    // CONFIG & TERRAIN
    // ═══════════════════════════════════════════════════════════════

    public void FindConfig() {
      Config = AssetDatabase.LoadAssetAtPath<WorldGeneratorConfigSO>(CONFIG_PATH);
      if (Config != null) SyncSeedFromConfig();
    }

    public void FindTerrain() {
      Terrain = Config?.terrain != null ? Config.terrain : Terrain.activeTerrain;
    }

    public void OnConfigChanged() {
      SyncSeedFromConfig();
      FindTerrain();
      Pipeline?.Reset();
      Pipeline = null;
      EnsurePipeline();
      
      // Clear gizmo caches when config changes
      BiomeOverlayGizmoDrawer.ClearCache();
      BiomeGizmoDrawer.ClearCache();
      
      NotifyStateChanged();
    }

    public void SyncSeedFromConfig() {
      if (Config != null) Seed = Config.Data.seed;
    }

    public void ApplySeedToConfig() {
      if (Config != null && Config.Data.seed != Seed) {
        Undo.RecordObject(Config, "Change Seed");
        Config.Data.seed = Seed;
        EditorUtility.SetDirty(Config);
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // PIPELINE
    // ═══════════════════════════════════════════════════════════════

    public void EnsurePipeline() {
      if (Pipeline == null) {
        Pipeline = new GenerationPipeline();
        SubscribePipelineEvents();
        InitTargetPhase();
      }
    }

    private void InitTargetPhase() {
      if (TargetPhaseIndex < 0 && Pipeline?.Phases.Count > 0) {
        TargetPhaseIndex = 0;
      }
    }

    private void SubscribePipelineEvents() {
      if (Pipeline == null) return;
      Pipeline.OnPhaseCompleted += HandlePhaseCompleted;
      Pipeline.OnPipelineCompleted += HandlePipelineCompleted;
      Pipeline.OnPipelineReset += HandlePipelineReset;
    }

    private void UnsubscribePipelineEvents() {
      if (Pipeline == null) return;
      Pipeline.OnPhaseCompleted -= HandlePhaseCompleted;
      Pipeline.OnPipelineCompleted -= HandlePipelineCompleted;
      Pipeline.OnPipelineReset -= HandlePipelineReset;
    }

    // ═══════════════════════════════════════════════════════════════
    // PHASE EXECUTION
    // ═══════════════════════════════════════════════════════════════

    public void RunPhase(int index) {
      EnsurePipeline();

      if (!Pipeline.IsRunning) {
        Pipeline.Begin(Config, Terrain, artistMode: true);
      }

      // If phase already completed, rollback first
      if (index <= Pipeline.CurrentPhaseIndex) {
        Pipeline.RollbackTo(index - 1);
      }

      // Execute phases up to and including target
      while (Pipeline.CurrentPhaseIndex < index && Pipeline.IsRunning) {
        Pipeline.ExecuteNextPhase();
      }

      NotifyStateChanged();
    }

    public void RollbackPhase(int index) {
      if (Pipeline == null || !Pipeline.IsRunning) return;
      Pipeline.RollbackTo(index - 1);
      NotifyStateChanged();
    }

    public void RunToTarget() {
      if (TargetPhaseIndex < 0) return;

      EnsurePipeline();

      // Always start fresh - reset and begin anew
      if (Pipeline.IsRunning) {
        Pipeline.Reset();
      }
      
      Pipeline.Begin(Config, Terrain, artistMode: true);

      // Execute phases up to and including target
      while (Pipeline.CurrentPhaseIndex < TargetPhaseIndex && Pipeline.IsRunning) {
        Pipeline.ExecuteNextPhase();
      }

      NotifyStateChanged();
    }

    public void Reset() {
      Pipeline?.Reset();
      NotifyStateChanged();
    }

    public void Clear() {
      Pipeline?.Reset();
      WorldGeneratorEditor.Clear();
      NotifyStateChanged();
    }

    // ═══════════════════════════════════════════════════════════════
    // CONDITIONS
    // ═══════════════════════════════════════════════════════════════

    public bool CanRunPhase(int index) {
      return Config != null && Terrain != null;
    }

    public bool CanRunToTarget() {
      return Config != null && Terrain != null && TargetPhaseIndex >= 0;
    }

    public bool CanReset() {
      return Pipeline != null && Pipeline.IsRunning;
    }

    public int GetCurrentPhaseIndex() {
      if (Pipeline?.CurrentPhaseIndex >= 0) return Pipeline.CurrentPhaseIndex;
      return TargetPhaseIndex >= 0 ? TargetPhaseIndex : 0;
    }

    // ═══════════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════════

    private void HandlePhaseCompleted(IGenerationPhase phase) {
      // Update river gizmo context for Phase 2+
      RiverGizmoDrawer.SetContext(Pipeline, Config, Terrain);
      
      // Force scene view repaint so gizmos update
      SceneView.RepaintAll();

      OnPhaseCompleted?.Invoke(phase);
      NotifyStateChanged();
    }

    private void HandlePipelineCompleted() {
      SceneView.RepaintAll();
      NotifyStateChanged();
    }

    private void HandlePipelineReset() {
      RiverGizmoDrawer.Clear();
      SceneView.RepaintAll();
      NotifyStateChanged();
    }

    private void NotifyStateChanged() {
      OnStateChanged?.Invoke();
    }
  }
}
#endif
