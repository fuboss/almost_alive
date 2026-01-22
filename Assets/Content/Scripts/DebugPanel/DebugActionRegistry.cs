using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Scripts.DebugPanel {
  public class DebugActionRegistry : IDisposable {
    private readonly List<IDebugAction> _actions = new();
    
    public event Action OnActionsChanged;

    public void Register(IDebugAction action) {
      if (!_actions.Contains(action)) {
        _actions.Add(action);
        OnActionsChanged?.Invoke();
      }
    }

    public void Unregister(IDebugAction action) {
      if (_actions.Remove(action)) {
        OnActionsChanged?.Invoke();
      }
    }

    public IEnumerable<IDebugAction> GetActionsByCategory(DebugCategory category) {
      return _actions.Where(a => a.category == category);
    }

    public IReadOnlyList<IDebugAction> GetAllActions() {
      return _actions.AsReadOnly();
    }

    public void Clear() {
      _actions.Clear();
      OnActionsChanged?.Invoke();
    }

    public void Dispose() {
      Clear();
    }
  }
}

