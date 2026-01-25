using System;
using System.Collections.Generic;
using Content.Scripts.Building.Runtime;
using Content.Scripts.Building.Runtime.Visuals;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Building.Services.Visuals {
  /// <summary>
  /// Centralized service for managing structure decorations visibility.
  /// Updates all structures via dirty tracking + Tick.
  /// </summary>
  public class StructureVisualsModule : ITickable, IDisposable {
    // Strategies
    [Inject] private IConstructionProgressionStrategy _progressionStrategy;
    [Inject] private IDecorationAnimationStrategy _animationStrategy;

    // Dirty tracking for optimization
    private readonly HashSet<Structure> _dirtyStructures = new();
    private readonly HashSet<UnfinishedStructureActor> _dirtyUnfinished = new();

    // Decoration caches per structure
    private readonly Dictionary<Structure, StructureDecoration[]> _structureDecorations = new();
    private readonly Dictionary<UnfinishedStructureActor, StructureDecoration[]> _unfinishedDecorations = new();


    #region Public API

    /// <summary>
    /// Mark structure as dirty for visuals update on next Tick.
    /// </summary>
    public void MarkDirty(Structure structure) {
      if (structure != null) {
        _dirtyStructures.Add(structure);
      }
    }

    /// <summary>
    /// Mark unfinished structure as dirty for visuals update on next Tick.
    /// </summary>
    public void MarkDirty(UnfinishedStructureActor unfinished) {
      if (unfinished != null) {
        _dirtyUnfinished.Add(unfinished);
      }
    }

    /// <summary>
    /// Force refresh all registered structures.
    /// </summary>
    public void RefreshAll() {
      // Refresh all cached structures
      foreach (var structure in _structureDecorations.Keys) {
        _dirtyStructures.Add(structure);
      }

      foreach (var unfinished in _unfinishedDecorations.Keys) {
        _dirtyUnfinished.Add(unfinished);
      }
    }

    #endregion

    #region ITickable

    public void Tick() {
      // Update dirty unfinished structures
      foreach (var unfinished in _dirtyUnfinished) {
        if (unfinished == null) continue;
        UpdateUnfinishedVisuals(unfinished);
      }

      _dirtyUnfinished.Clear();

      // Update dirty structures
      foreach (var structure in _dirtyStructures) {
        if (structure == null) continue;
        UpdateStructureVisuals(structure);
      }

      _dirtyStructures.Clear();
    }

    #endregion

    #region Private Update Methods

    private void UpdateUnfinishedVisuals(UnfinishedStructureActor unfinished) {
      // Get or cache decorations
      if (!_unfinishedDecorations.TryGetValue(unfinished, out var decorations)) {
        decorations = unfinished.GetComponentsInChildren<StructureDecoration>(true);
        _unfinishedDecorations[unfinished] = decorations;
      }

      // Calculate progress
      var progress = unfinished.workRequired > 0
        ? unfinished.workProgress / unfinished.workRequired
        : 0f;

      var context = VisualsContext.ForUnfinished(unfinished, progress);

      // Update each decoration
      foreach (var decoration in decorations) {
        if (decoration == null) continue;
        UpdateDecoration(decoration, context);
      }
    }

    private void UpdateStructureVisuals(Structure structure) {
      // Get or cache decorations
      if (!_structureDecorations.TryGetValue(structure, out var decorations)) {
        decorations = structure.GetComponentsInChildren<StructureDecoration>(true);
        _structureDecorations[structure] = decorations;
      }

      var context = VisualsContext.ForStructure(structure);

      // Update each decoration
      foreach (var decoration in decorations) {
        if (decoration == null) continue;
        UpdateDecoration(decoration, context);
      }
    }

    private void UpdateDecoration(StructureDecoration decoration, VisualsContext context) {
      // Apply progression strategy for construction mode
      if (context.isUnfinished && decoration.visibilityMode == DecorationVisibilityMode.OnConstruction) {
        var effectiveProgress = _progressionStrategy.GetEffectiveProgress(
          context.constructionProgress,
          decoration
        );

        // Update context with effective progress
        context.constructionProgress = effectiveProgress;
      }

      // Evaluate visibility
      var shouldBeVisible = decoration.ShouldBeVisible(context);

      // Apply change if needed
      if (shouldBeVisible != decoration.isVisible) {
        decoration.SetVisible(shouldBeVisible, _animationStrategy);
      }
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Clean up cache for destroyed structures.
    /// Call this periodically or on structure destroy.
    /// </summary>
    public void CleanupCache() {
      // Remove null entries from caches
      var structuresToRemove = new List<Structure>();
      foreach (var kvp in _structureDecorations) {
        if (kvp.Key == null) {
          structuresToRemove.Add(kvp.Key);
        }
      }

      foreach (var structure in structuresToRemove) {
        _structureDecorations.Remove(structure);
      }

      var unfinishedToRemove = new List<UnfinishedStructureActor>();
      foreach (var kvp in _unfinishedDecorations) {
        if (kvp.Key == null) {
          unfinishedToRemove.Add(kvp.Key);
        }
      }

      foreach (var unfinished in unfinishedToRemove) {
        _unfinishedDecorations.Remove(unfinished);
      }
    }

    #endregion

    public void Dispose() {
      CleanupCache();
    }
  }
}