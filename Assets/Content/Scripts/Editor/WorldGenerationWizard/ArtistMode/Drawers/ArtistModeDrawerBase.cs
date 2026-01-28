#if UNITY_EDITOR

namespace Content.Scripts.Editor.WorldGenerationWizard.ArtistMode.Drawers {
  /// <summary>
  /// Base class for Artist Mode section drawers.
  /// Each drawer renders one section of the window.
  /// </summary>
  public abstract class ArtistModeDrawerBase {
    protected ArtistModeState State { get; }

    protected ArtistModeDrawerBase(ArtistModeState state) {
      State = state;
    }

    public abstract void Draw();
  }
}
#endif
