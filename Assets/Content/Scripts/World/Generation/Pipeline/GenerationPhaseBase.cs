using System;
using UnityEngine;

namespace Content.Scripts.World.Generation.Pipeline {
  /// <summary>
  /// Abstract base class for generation phases.
  /// Provides common functionality and template methods.
  /// </summary>
  public abstract class GenerationPhaseBase : IGenerationPhase {
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    
    public PhaseState State { get; protected set; } = PhaseState.Pending;
    public float Progress { get; protected set; }
    
    public event Action<PhaseState> OnStateChanged;
    public event Action<float> OnProgressChanged;

    // ═══════════════════════════════════════════════════════════════
    // TEMPLATE METHODS (override in derived classes)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Main execution logic. Override in derived classes.
    /// </summary>
    protected abstract void ExecuteInternal(GenerationContext ctx);
    
    /// <summary>
    /// Cleanup logic for rollback. Override in derived classes.
    /// </summary>
    protected abstract void RollbackInternal(GenerationContext ctx);
    
    /// <summary>
    /// Create debug visualization material. Override if needed.
    /// </summary>
    protected virtual Material CreateDebugMaterial(GenerationContext ctx) => null;
    
    /// <summary>
    /// Validation before execution. Override to add checks.
    /// </summary>
    protected virtual bool ValidateContext(GenerationContext ctx) => ctx != null;

    // ═══════════════════════════════════════════════════════════════
    // IGenerationPhase IMPLEMENTATION
    // ═══════════════════════════════════════════════════════════════

    public void Execute(GenerationContext ctx) {
      if (!CanExecute(ctx)) {
        SetState(PhaseState.Skipped);
        return;
      }
      
      try {
        SetState(PhaseState.Running);
        SetProgress(0f);
        
        ExecuteInternal(ctx);
        
        SetProgress(1f);
        SetState(PhaseState.Completed);
        
        if (ctx.Config.logGeneration) {
          Debug.Log($"[WorldGen] Phase completed: {Name}");
        }
      }
      catch (Exception ex) {
        Debug.LogError($"[WorldGen] Phase failed: {Name}\n{ex}");
        SetState(PhaseState.Failed);
      }
    }

    public void Rollback(GenerationContext ctx) {
      try {
        RollbackInternal(ctx);
        SetState(PhaseState.Pending);
        SetProgress(0f);
        
        if (ctx.Config.logGeneration) {
          Debug.Log($"[WorldGen] Phase rolled back: {Name}");
        }
      }
      catch (Exception ex) {
        Debug.LogError($"[WorldGen] Rollback failed: {Name}\n{ex}");
      }
    }

    public Material GetDebugMaterial(GenerationContext ctx) {
      return CreateDebugMaterial(ctx);
    }

    public bool CanExecute(GenerationContext ctx) {
      return ValidateContext(ctx);
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    protected void SetState(PhaseState newState) {
      if (State != newState) {
        State = newState;
        OnStateChanged?.Invoke(newState);
      }
    }

    protected void SetProgress(float value) {
      Progress = Mathf.Clamp01(value);
      OnProgressChanged?.Invoke(Progress);
    }

    /// <summary>
    /// Report progress during long operations.
    /// Call this periodically from ExecuteInternal.
    /// </summary>
    protected void ReportProgress(float value, string message = null) {
      SetProgress(value);
      
      #if UNITY_EDITOR
      if (!string.IsNullOrEmpty(message)) {
        UnityEditor.EditorUtility.DisplayProgressBar($"[{Name}]", message, value);
      }
      #endif
    }

    /// <summary>
    /// Clear any progress bar shown during execution.
    /// </summary>
    protected void ClearProgressBar() {
      #if UNITY_EDITOR
      UnityEditor.EditorUtility.ClearProgressBar();
      #endif
    }
  }
}
