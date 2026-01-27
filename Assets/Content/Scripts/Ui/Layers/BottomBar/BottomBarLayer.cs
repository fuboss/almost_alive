using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.BottomBar {
  /// <summary>
  /// Bottom bar with command category buttons.
  /// Each category opens a submenu with commands.
  /// If category has single command, executes it directly.
  /// Layout: [Build][Orders][Work][Zones][Debug] ←spacer→ [Menu]
  /// </summary>
  public class BottomBarLayer : UILayer {
    [Header("Layout")]
    [SerializeField] private Transform _container;
    [SerializeField] private CommandCategoryButton _categoryButtonPrefab;
    [SerializeField] private GameObject _spacerPrefab;

    [Header("Submenu")]
    [SerializeField] private CommandSubmenu _submenu;

    [Header("Category Config")]
    [SerializeField] private List<CategoryConfig> _categoryConfigs = new();

    private readonly Dictionary<CommandCategory, CommandCategoryButton> _buttons = new();
    private CommandCategory? _openCategory;

    public override void Initialize() {
      base.Initialize();
      
      BuildCategoryButtons();
      CommandRegistry.OnCommandsChanged += OnCommandsChanged;
      
      if (_submenu != null) {
        _submenu.OnCommandExecuted += OnSubmenuCommandExecuted;
        _submenu.Hide();
      }

      Show();
    }

    private void OnDestroy() {
      CommandRegistry.OnCommandsChanged -= OnCommandsChanged;
      if (_submenu != null)
        _submenu.OnCommandExecuted -= OnSubmenuCommandExecuted;
    }

    private void BuildCategoryButtons() {
      foreach (var btn in _buttons.Values) {
        if (btn != null) Destroy(btn.gameObject);
      }
      _buttons.Clear();

      bool spacerInserted = false;

      foreach (var config in _categoryConfigs) {
        if (config.alignRight && !spacerInserted) {
          InsertSpacer();
          spacerInserted = true;
        }

        var btn = Instantiate(_categoryButtonPrefab, _container);
        btn.Setup(config.category, config.label, config.icon, OnCategoryClicked);
        _buttons[config.category] = btn;
      }

      RefreshButtonStates();
    }

    private void InsertSpacer() {
      if (_spacerPrefab != null) {
        Instantiate(_spacerPrefab, _container);
      }
      else {
        var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(_container, false);
        var layout = spacer.GetComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
      }
    }

    private void OnCommandsChanged() {
      RefreshButtonStates();
    }

    private void RefreshButtonStates() {
      foreach (var kvp in _buttons) {
        var commands = CommandRegistry.GetByCategory(kvp.Key);
        var hasCommands = commands.Count > 0;
        kvp.Value.SetInteractable(hasCommands);
      }
    }

    private void OnCategoryClicked(CommandCategory category) {
      var commands = CommandRegistry.GetByCategory(category);
      
      // If single command — execute directly
      if (commands.Count == 1) {
        var cmd = commands[0];
        if (cmd.CanExecute()) {
          cmd.Execute();
        }
        return;
      }

      // Toggle submenu
      if (_openCategory == category) {
        CloseSubmenu();
        return;
      }

      OpenSubmenu(category);
    }

    private void OpenSubmenu(CommandCategory category) {
      if (_submenu == null) return;

      var commands = CommandRegistry.GetByCategory(category);
      if (commands.Count == 0) return;

      if (_buttons.TryGetValue(category, out var btn)) {
        _submenu.Show(commands, btn.transform as RectTransform);
      }

      _openCategory = category;
      UpdateButtonSelection();
    }

    private void CloseSubmenu() {
      _submenu?.Hide();
      _openCategory = null;
      UpdateButtonSelection();
    }

    private void OnSubmenuCommandExecuted() {
      // Keep submenu open for multi-selection scenarios
    }

    private void UpdateButtonSelection() {
      foreach (var kvp in _buttons) {
        kvp.Value.SetSelected(kvp.Key == _openCategory);
      }
    }

    public override void OnUpdate() {
      base.OnUpdate();

      if (_openCategory == null) return;

      if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) {
        CloseSubmenu();
      }
    }

    [System.Serializable]
    public class CategoryConfig {
      public CommandCategory category;
      public string label;
      public string icon;
      [Tooltip("If true, spacer will be inserted before this button")]
      public bool alignRight;
    }
  }
}
