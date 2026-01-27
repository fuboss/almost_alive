using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Interaction;

namespace Content.Scripts.Ui.Services {
  /// <summary>
  /// Central selection service. Works with any ISelectableActor.
  /// Implements IAgentSelectionModule for backward compat with agent-specific code.
  /// </summary>
  public class SelectionService : IAgentSelectionModule, IDisposable {
    private ISelectableActor _current;
    private ISelectableActor _prev;

    // Generic selection event
    public event Action<ISelectableActor, ISelectableActor> OnSelected;
    
    // IAgentSelectionModule (backward compat)
    public event Action<IGoapAgent, IGoapAgent> OnSelectionChanged;

    public ISelectableActor current => _current;

    public IGoapAgent GetSelectedAgent() => _current as IGoapAgent;

    public void Select(ISelectableActor target) {
      if (_current == target) return;
      if (target != null && !target.canSelect) return;

      // Update flags
      if (_current != null) _current.isSelected = false;

      _prev = _current;
      _current = target;

      if (_current != null) _current.isSelected = true;

      // Fire events
      OnSelected?.Invoke(_current, _prev);
      OnSelectionChanged?.Invoke(_current as IGoapAgent, _prev as IGoapAgent);
    }

    public void SelectAgent(IGoapAgent agent) => Select(agent as ISelectableActor);

    public void SelectAgents(params IGoapAgent[] agents) {
      if (agents == null || agents.Length == 0) {
        ClearSelection();
        return;
      }
      SelectAgent(agents[0]);
    }

    public void ClearSelection() => Select(null);

    public void Dispose() {
      OnSelected = null;
      OnSelectionChanged = null;
      _current = null;
      _prev = null;
    }
  }
}
