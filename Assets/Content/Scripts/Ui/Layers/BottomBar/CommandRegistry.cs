using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Scripts.Ui.Layers.BottomBar {
  /// <summary>
  /// Central registry for bottom bar commands.
  /// Modules register their commands here.
  /// </summary>
  public static class CommandRegistry {
    private static readonly Dictionary<string, ICommand> _commands = new();
    private static readonly Dictionary<CommandCategory, List<ICommand>> _byCategory = new();

    /// <summary>
    /// Fired when commands change. UI should refresh.
    /// </summary>
    public static event Action OnCommandsChanged;

    /// <summary>
    /// Register a command.
    /// </summary>
    public static void Register(ICommand command) {
      if (_commands.ContainsKey(command.id)) {
        UnityEngine.Debug.LogWarning($"[CommandRegistry] Command '{command.id}' already registered, replacing");
        Unregister(command.id);
      }

      _commands[command.id] = command;

      if (!_byCategory.TryGetValue(command.category, out var list)) {
        list = new List<ICommand>();
        _byCategory[command.category] = list;
      }
      list.Add(command);
      list.Sort((a, b) => a.order.CompareTo(b.order));

      OnCommandsChanged?.Invoke();
    }

    /// <summary>
    /// Unregister a command by id.
    /// </summary>
    public static void Unregister(string id) {
      if (!_commands.TryGetValue(id, out var command)) return;

      _commands.Remove(id);

      if (_byCategory.TryGetValue(command.category, out var list)) {
        list.Remove(command);
      }

      OnCommandsChanged?.Invoke();
    }

    /// <summary>
    /// Get all commands in a category, sorted by order.
    /// </summary>
    public static IReadOnlyList<ICommand> GetByCategory(CommandCategory category) {
      return _byCategory.TryGetValue(category, out var list) 
        ? list 
        : Array.Empty<ICommand>();
    }

    /// <summary>
    /// Get all categories that have at least one command.
    /// </summary>
    public static IEnumerable<CommandCategory> GetActiveCategories() {
      return _byCategory
        .Where(kvp => kvp.Value.Count > 0)
        .Select(kvp => kvp.Key)
        .OrderBy(c => (int)c);
    }

    /// <summary>
    /// Get command by id.
    /// </summary>
    public static ICommand Get(string id) {
      return _commands.GetValueOrDefault(id);
    }

    /// <summary>
    /// Clear all commands. Call on scene unload.
    /// </summary>
    public static void Clear() {
      _commands.Clear();
      _byCategory.Clear();
      OnCommandsChanged?.Invoke();
    }
  }
}
