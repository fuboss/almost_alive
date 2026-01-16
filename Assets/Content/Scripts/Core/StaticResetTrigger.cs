using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Content.Scripts.Core {
  /// <summary>
  /// Triggers static registry reset on Play Mode enter/exit.
  /// Handles both domain reload ON and OFF scenarios.
  /// </summary>
  public static class StaticResetTrigger {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void OnSubsystemRegistration() {
      // Called before scene load, domain may or may not have reloaded
      StaticResetRegistry.Initialize();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorInit() {
      EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
      EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state) {
      switch (state) {
        case PlayModeStateChange.ExitingPlayMode:
          StaticResetRegistry.Shutdown();
          break;
        case PlayModeStateChange.ExitingEditMode:
          // About to enter play mode - reset will happen in SubsystemRegistration
          break;
      }
    }
#endif
  }
}
