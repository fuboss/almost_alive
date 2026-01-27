using Content.Scripts.Game.Interaction;
using UnityEngine;

namespace Content.Scripts.Ui.Services {
  public enum SelectableType { None, Agent, Building, Resource }

  /// <summary>
  /// Extended selectable with type info. Inherits from existing ISelectableActor.
  /// </summary>
  public interface ISelectable : ISelectableActor {
    Transform transform { get; }
    SelectableType selectableType { get; }
    string displayName { get; }
  }
}
