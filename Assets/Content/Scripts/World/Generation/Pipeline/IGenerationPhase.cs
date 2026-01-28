using System;
using UnityEngine;

namespace Content.Scripts.World.Generation.Pipeline {
  /// <summary>
  /// State of a generation phase.
  /// </summary>
  public enum PhaseState {
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
  }

  /// <summary>
  /// Interface for a single generation phase.
  /// Each phase performs one step of world generation.
  /// </summary>
  public interface IGenerationPhase {
    /// <summary>Display name for UI</summary>
    string Name { get; }
    
    /// <summary>Short description for tooltips</summary>
    string Description { get; }
    
    /// <summary>Current execution state</summary>
    PhaseState State { get; }
    
    /// <summary>Progress 0-1 for async operations</summary>
    float Progress { get; }
    
    /// <summary>
    /// Execute this phase. May be async for long operations.
    /// </summary>
    /// <param name="ctx">Shared generation context</param>
    void Execute(GenerationContext ctx);
    
    /// <summary>
    /// Rollback changes made by this phase.
    /// Called when user wants to re-do this phase.
    /// </summary>
    /// <param name="ctx">Shared generation context</param>
    void Rollback(GenerationContext ctx);
    
    /// <summary>
    /// Get debug visualization material for Artist Mode.
    /// Return null to keep current terrain material.
    /// </summary>
    Material GetDebugMaterial(GenerationContext ctx);
    
    /// <summary>
    /// Check if this phase can be executed given current context.
    /// </summary>
    bool CanExecute(GenerationContext ctx);
    
    /// <summary>
    /// Event fired when phase state changes.
    /// </summary>
    event Action<PhaseState> OnStateChanged;
    
    /// <summary>
    /// Event fired when progress updates (for async phases).
    /// </summary>
    event Action<float> OnProgressChanged;
  }
}
