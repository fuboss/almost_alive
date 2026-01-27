using System;
using System.Collections.Generic;
using Content.Scripts.Game.Interaction;

namespace Content.Scripts.Ui.Layers.Inspector {
  /// <summary>
  /// Context action that can be performed on a selected actor.
  /// </summary>
  public interface IContextAction {
    string label { get; }
    string icon { get; } // emoji or icon name
    
    /// <summary>
    /// Can this action be performed on the target?
    /// </summary>
    bool CanExecute(ISelectableActor target);
    
    /// <summary>
    /// Execute the action.
    /// </summary>
    void Execute(ISelectableActor target);
  }

  /// <summary>
  /// Simple implementation for lambda-based actions.
  /// </summary>
  public class ContextAction : IContextAction {
    public string label { get; }
    public string icon { get; }
    
    private readonly Func<ISelectableActor, bool> _canExecute;
    private readonly Action<ISelectableActor> _execute;

    public ContextAction(string label, string icon, 
      Func<ISelectableActor, bool> canExecute, 
      Action<ISelectableActor> execute) {
      this.label = label;
      this.icon = icon;
      _canExecute = canExecute;
      _execute = execute;
    }

    public bool CanExecute(ISelectableActor target) => _canExecute?.Invoke(target) ?? true;
    public void Execute(ISelectableActor target) => _execute?.Invoke(target);
  }

  /// <summary>
  /// Registry for context actions. Actions are registered by actor tags.
  /// </summary>
  public static class ContextActionRegistry {
    private static readonly Dictionary<string, List<IContextAction>> _actionsByTag = new();
    private static readonly List<IContextAction> _globalActions = new();

    /// <summary>
    /// Register action for actors with specific tag.
    /// </summary>
    public static void RegisterForTag(string tag, IContextAction action) {
      if (!_actionsByTag.TryGetValue(tag, out var list)) {
        list = new List<IContextAction>();
        _actionsByTag[tag] = list;
      }
      list.Add(action);
    }

    /// <summary>
    /// Register action available for all actors.
    /// </summary>
    public static void RegisterGlobal(IContextAction action) {
      _globalActions.Add(action);
    }

    /// <summary>
    /// Get all applicable actions for an actor.
    /// </summary>
    public static IEnumerable<IContextAction> GetActionsFor(ISelectableActor actor) {
      var result = new List<IContextAction>();

      // Global actions
      foreach (var action in _globalActions) {
        if (action.CanExecute(actor))
          result.Add(action);
      }

      // Tag-specific actions
      var actorDesc = actor.gameObject.GetComponent<Game.ActorDescription>();
      if (actorDesc?.descriptionData?.tags != null) {
        foreach (var tag in actorDesc.descriptionData.tags) {
          if (_actionsByTag.TryGetValue(tag, out var actions)) {
            foreach (var action in actions) {
              if (action.CanExecute(actor))
                result.Add(action);
            }
          }
        }
      }

      return result;
    }

    /// <summary>
    /// Clear all registered actions. Call on scene unload.
    /// </summary>
    public static void Clear() {
      _actionsByTag.Clear();
      _globalActions.Clear();
    }
  }
}
