using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.BottomBar {
  /// <summary>
  /// Single item in command submenu.
  /// Supports commands, groups (folders), and back navigation.
  /// </summary>
  public class CommandMenuItem : MonoBehaviour {
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _labelText;
    [SerializeField] private TMP_Text _iconText;
    [SerializeField] private Image _background;

    [Header("Colors")]
    [SerializeField] private Color _normalColor = new(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color _groupColor = new(0.2f, 0.28f, 0.2f, 1f);
    [SerializeField] private Color _backColor = new(0.28f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color _disabledTextColor = new(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color _normalTextColor = new(0.83f, 0.83f, 0.83f, 1f);

    private CommandSubmenu.MenuEntry _entry;
    private Action<CommandSubmenu.MenuEntry> _onClick;

    public void Setup(CommandSubmenu.MenuEntry entry, Action<CommandSubmenu.MenuEntry> onClick) {
      _entry = entry;
      _onClick = onClick;

      if (_labelText != null) {
        _labelText.text = entry.displayText;
        _labelText.alignment = entry.isGroup || entry.isBack 
          ? TextAlignmentOptions.MidlineLeft 
          : TextAlignmentOptions.Center;
      }

      if (_iconText != null) {
        if (entry.isBack) {
          _iconText.text = "";
        }
        else if (entry.isGroup) {
          _iconText.text = "";
        }
        else if (entry.command != null) {
          _iconText.text = entry.command.icon;
        }
      }

      _button.onClick.RemoveAllListeners();
      _button.onClick.AddListener(OnClick);

      UpdateVisuals();
    }

    /// <summary>Setup from legacy ICommand (for backward compat).</summary>
    public void Setup(ICommand command, Action<ICommand> onClick) {
      var entry = new CommandSubmenu.MenuEntry {
        displayText = command.label,
        command = command
      };
      Setup(entry, e => onClick?.Invoke(e.command));
    }

    public void Refresh() {
      UpdateVisuals();
    }

    private void OnClick() {
      _onClick?.Invoke(_entry);
    }

    private void UpdateVisuals() {
      if (_background != null) {
        if (_entry.isBack) {
          _background.color = _backColor;
        }
        else if (_entry.isGroup) {
          _background.color = _groupColor;
        }
        else {
          _background.color = _normalColor;
        }
      }

      // Update interactability for commands
      if (_entry.command != null) {
        var canExecute = _entry.command.CanExecute();
        _button.interactable = canExecute;

        if (_labelText != null) {
          _labelText.color = canExecute ? _normalTextColor : _disabledTextColor;
        }
      }
      else {
        // Groups and back are always interactable
        _button.interactable = true;
        if (_labelText != null) {
          _labelText.color = _normalTextColor;
        }
      }
    }
  }
}
