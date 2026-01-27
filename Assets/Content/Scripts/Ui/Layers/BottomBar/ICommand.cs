using System;

namespace Content.Scripts.Ui.Layers.BottomBar {
  /// <summary>
  /// Command category for bottom bar grouping.
  /// </summary>
  public enum CommandCategory {
    Build,
    Orders,
    Work,
    Zones,
    Debug,
    Menu
  }

  /// <summary>
  /// Interface for executable commands.
  /// </summary>
  public interface ICommand {
    string id { get; }
    string label { get; }
    string icon { get; }
    string tooltip { get; }
    CommandCategory category { get; }
    
    /// <summary>
    /// Order within category (lower = first).
    /// </summary>
    int order { get; }
    
    /// <summary>
    /// Is command currently available?
    /// </summary>
    bool CanExecute();
    
    /// <summary>
    /// Execute the command.
    /// </summary>
    void Execute();
  }

  /// <summary>
  /// Simple command implementation with lambdas.
  /// </summary>
  public class Command : ICommand {
    public string id { get; }
    public string label { get; }
    public string icon { get; }
    public string tooltip { get; }
    public CommandCategory category { get; }
    public int order { get; }

    private readonly Func<bool> _canExecute;
    private readonly Action _execute;

    public Command(
      string id,
      string label,
      string icon,
      CommandCategory category,
      Action execute,
      Func<bool> canExecute = null,
      string tooltip = null,
      int order = 0) {
      this.id = id;
      this.label = label;
      this.icon = icon;
      this.category = category;
      this.order = order;
      this.tooltip = tooltip ?? label;
      _execute = execute;
      _canExecute = canExecute;
    }

    public bool CanExecute() => _canExecute?.Invoke() ?? true;
    public void Execute() => _execute?.Invoke();
  }
}
