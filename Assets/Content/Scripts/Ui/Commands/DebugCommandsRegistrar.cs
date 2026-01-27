using Content.Scripts.DebugPanel;
using Content.Scripts.Ui.Layers.BottomBar;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Ui.Commands {
  /// <summary>
  /// Debug category just toggles the DebugPanel (same as F12).
  /// All debug actions are in DebugPanel itself with hierarchical menu.
  /// </summary>
  public class DebugCommandsRegistrar : IStartable {
    [Inject] private DebugModule _debugModule;

    public void Start() {
      // Single command — toggle debug panel
      CommandRegistry.Register(new Command(
        id: "debug.toggle",
        label: "Debug Panel (F12)",
        icon: "🔧",
        category: CommandCategory.Debug,
        execute: () => _debugModule.TogglePanel(),
        order: 0
      ));
    }
  }
}
