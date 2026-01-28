using System;
using System.Collections.Generic;
using Content.Scripts.World.Generation.Pipeline.Phases;
using UnityEngine;

namespace Content.Scripts.World.Generation.Pipeline {
  /// <summary>
  /// Orchestrates the world generation pipeline.
  /// Manages phase execution, pausing (Artist Mode), and rollback.
  /// </summary>
  public class GenerationPipeline {
    
    // ═══════════════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════════════

    private readonly List<IGenerationPhase> _phases = new();
    
    public IReadOnlyList<IGenerationPhase> Phases => _phases;
    public int CurrentPhaseIndex { get; private set; } = -1;
    public bool IsPaused { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsCompleted => CurrentPhaseIndex >= _phases.Count - 1 && 
                                CurrentPhase?.State == PhaseState.Completed;
    
    public GenerationContext Context { get; private set; }
    
    public IGenerationPhase CurrentPhase => 
      CurrentPhaseIndex >= 0 && CurrentPhaseIndex < _phases.Count 
        ? _phases[CurrentPhaseIndex] 
        : null;

    // ═══════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════

    public event Action<IGenerationPhase> OnPhaseStarted;
    public event Action<IGenerationPhase> OnPhaseCompleted;
    public event Action<IGenerationPhase> OnPhaseFailed;
    public event Action OnPipelineStarted;
    public event Action OnPipelineCompleted;
    public event Action OnPipelinePaused;
    public event Action OnPipelineReset;

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════

    public GenerationPipeline() {
      RegisterDefaultPhases();
    }

    private void RegisterDefaultPhases() {
      _phases.Add(new BiomeLayoutPhase());
      _phases.Add(new TerrainSculptPhase());
      _phases.Add(new SplatmapPaintPhase());
      _phases.Add(new VegetationPhase());
      _phases.Add(new ScatterPhase());
    }

    // ═══════════════════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Start the generation pipeline.
    /// </summary>
    /// <param name="config">World generation config</param>
    /// <param name="terrain">Target terrain</param>
    /// <param name="artistMode">If true, pause after each phase</param>
    public void Begin(WorldGeneratorConfigSO config, Terrain terrain, bool artistMode = false) {
      if (IsRunning) {
        Debug.LogWarning("[WorldGen] Pipeline already running");
        return;
      }
      
      // Create fresh context
      Context = new GenerationContext(config, terrain) {
        IsArtistMode = artistMode
      };
      
      CurrentPhaseIndex = -1;
      IsPaused = false;
      IsRunning = true;
      
      OnPipelineStarted?.Invoke();
      
      if (config.Data.logGeneration) {
        Debug.Log($"[WorldGen] Pipeline started (seed: {Context.Seed}, artistMode: {artistMode})");
      }
      
      if (artistMode) {
        // Execute first phase then pause
        ExecuteNextPhase();
      } else {
        // Run all phases without stopping
        ExecuteAll();
      }
    }

    /// <summary>
    /// Execute the next pending phase.
    /// In Artist Mode, will pause after completion.
    /// </summary>
    public void ExecuteNextPhase() {
      if (!IsRunning) {
        Debug.LogWarning("[WorldGen] Pipeline not running");
        return;
      }
      
      if (CurrentPhaseIndex >= _phases.Count - 1) {
        Finalize();
        return;
      }
      
      IsPaused = false;
      CurrentPhaseIndex++;
      
      var phase = _phases[CurrentPhaseIndex];
      OnPhaseStarted?.Invoke(phase);
      
      phase.Execute(Context);
      
      if (phase.State == PhaseState.Completed) {
        OnPhaseCompleted?.Invoke(phase);
        
        // Apply debug visualization in artist mode
        if (Context.IsArtistMode) {
          var debugMat = phase.GetDebugMaterial(Context);
          Context.SetDebugMaterial(debugMat);
          
          IsPaused = true;
          OnPipelinePaused?.Invoke();
        }
        
        // Check if done
        if (CurrentPhaseIndex >= _phases.Count - 1) {
          Finalize();
        }
      }
      else if (phase.State == PhaseState.Failed) {
        OnPhaseFailed?.Invoke(phase);
        IsRunning = false;
      }
    }

    /// <summary>
    /// Execute all remaining phases without pausing.
    /// </summary>
    public void ExecuteAll() {
      var wasArtistMode = Context?.IsArtistMode ?? false;
      if (Context != null) {
        Context.IsArtistMode = false; // Temporarily disable for RunAll
      }
      
      while (IsRunning && CurrentPhaseIndex < _phases.Count - 1) {
        ExecuteNextPhase();
      }
      
      if (Context != null) {
        Context.IsArtistMode = wasArtistMode;
      }
    }

    /// <summary>
    /// Rollback to before the specified phase index.
    /// Phase at targetIndex will be pending (not executed).
    /// </summary>
    public void RollbackTo(int targetIndex) {
      if (Context == null) return;
      
      targetIndex = Mathf.Clamp(targetIndex, -1, _phases.Count - 1);
      
      // Rollback phases in reverse order
      for (int i = CurrentPhaseIndex; i > targetIndex; i--) {
        if (i >= 0 && i < _phases.Count) {
          _phases[i].Rollback(Context);
        }
      }
      
      CurrentPhaseIndex = targetIndex;
      IsPaused = Context.IsArtistMode;
      
      // Update debug visualization
      if (Context.IsArtistMode && targetIndex >= 0) {
        var phase = _phases[targetIndex];
        var debugMat = phase.GetDebugMaterial(Context);
        Context.SetDebugMaterial(debugMat);
      } else {
        Context.SetDebugMaterial(null);
      }
    }

    /// <summary>
    /// Reset pipeline completely.
    /// </summary>
    public void Reset() {
      if (Context != null) {
        // Rollback all phases
        for (int i = CurrentPhaseIndex; i >= 0; i--) {
          _phases[i].Rollback(Context);
        }
        
        // Cleanup debug visualization
        Context.CleanupDebugVisualization();
        
        // Restore original terrain data
        Context.RestoreTerrainData();
      }
      
      CurrentPhaseIndex = -1;
      IsPaused = false;
      IsRunning = false;
      
      OnPipelineReset?.Invoke();
      
      Debug.Log("[WorldGen] Pipeline reset");
    }

    /// <summary>
    /// Continue execution after pause (Artist Mode).
    /// </summary>
    public void Continue() {
      if (IsPaused && IsRunning) {
        ExecuteNextPhase();
      }
    }

    // ═══════════════════════════════════════════════════════════════
    // PRIVATE
    // ═══════════════════════════════════════════════════════════════

    private void Finalize() {
      // Cleanup debug visualization
      Context?.CleanupDebugVisualization();
      
      IsRunning = false;
      IsPaused = false;
      
      OnPipelineCompleted?.Invoke();
      
      if (Context?.Config?.logGeneration ?? false) {
        Debug.Log("[WorldGen] Pipeline completed");
      }
    }
  }
}
