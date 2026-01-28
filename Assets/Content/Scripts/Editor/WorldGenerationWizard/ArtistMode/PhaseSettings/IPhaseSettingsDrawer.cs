#if UNITY_EDITOR
using Content.Scripts.World;
using UnityEngine;

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.PhaseSettings {
  /// <summary>
  /// Interface for phase-specific settings drawers.
  /// Each generation phase can have custom settings UI.
  /// </summary>
  public interface IPhaseSettingsDrawer {
    string PhaseName { get; }
    int PhaseIndex { get; }
    bool IsFoldedOut { get; set; }
    void Draw(WorldGeneratorConfigSO config, GUIStyle boxStyle);
  }
}
#endif
