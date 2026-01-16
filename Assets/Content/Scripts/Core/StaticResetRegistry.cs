using System;
using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.Core {
  /// <summary>
  /// Collects reset actions from static classes (including generics).
  /// Call RegisterReset() from static constructors.
  /// </summary>
  public static class StaticResetRegistry {
    private static readonly List<Action> _resetActions = new();
    private static bool _initialized;

    public static void RegisterReset(Action resetAction) {
      if (resetAction == null) return;
      if (!_resetActions.Contains(resetAction)) {
        _resetActions.Add(resetAction);
      }
    }

    public static void ResetAll() {
      Debug.Log($"[StaticResetRegistry] Resetting {_resetActions.Count} static registries...");
      foreach (var action in _resetActions) {
        try {
          action?.Invoke();
        }
        catch (Exception e) {
          Debug.LogException(e);
        }
      }
    }

    public static int registeredCount => _resetActions.Count;

    // Called by StaticResetTrigger
    internal static void Initialize() {
      if (_initialized) return;
      _initialized = true;
      ResetAll();
    }

    internal static void Shutdown() {
      _initialized = false;
    }
  }
}
