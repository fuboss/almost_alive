using Content.Scripts.Game.Interaction;
using UnityEngine;

namespace Content.Scripts.Ui.Layers.Inspector {
  /// <summary>
  /// Interface for inspector views. Each view handles specific actor types.
  /// </summary>
  public interface IInspectorView {
    GameObject gameObject { get; }
    
    /// <summary>
    /// Can this view display the given actor?
    /// </summary>
    bool CanHandle(ISelectableActor actor);
    
    /// <summary>
    /// Show view with the given actor.
    /// </summary>
    void Show(ISelectableActor actor);
    
    /// <summary>
    /// Hide this view.
    /// </summary>
    void Hide();
    
    /// <summary>
    /// Called every frame while visible.
    /// </summary>
    void OnUpdate();
  }
}
