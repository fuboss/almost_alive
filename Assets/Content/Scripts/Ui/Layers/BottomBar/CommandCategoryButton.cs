using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.BottomBar {
  /// <summary>
  /// Button for command category in bottom bar.
  /// </summary>
  public class CommandCategoryButton : MonoBehaviour {
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _labelText;
    [SerializeField] private TMP_Text _iconText;
    [SerializeField] private Image _background;

    [Header("Colors")]
    [SerializeField] private Color _normalColor = new(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color _selectedColor = new(0.25f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color _disabledColor = new(0.1f, 0.1f, 0.1f, 0.5f);

    private CommandCategory _category;
    private Action<CommandCategory> _onClick;
    private bool _isSelected;

    public void Setup(CommandCategory category, string label, string icon, Action<CommandCategory> onClick) {
      _category = category;
      _onClick = onClick;

      if (_labelText != null) _labelText.text = label;
      if (_iconText != null) _iconText.text = icon;

      _button.onClick.RemoveAllListeners();
      _button.onClick.AddListener(OnClick);

      UpdateVisuals();
    }

    public void SetInteractable(bool interactable) {
      _button.interactable = interactable;
      UpdateVisuals();
    }

    public void SetSelected(bool selected) {
      _isSelected = selected;
      UpdateVisuals();
    }

    private void OnClick() {
      _onClick?.Invoke(_category);
    }

    private void UpdateVisuals() {
      if (_background == null) return;

      if (!_button.interactable) {
        _background.color = _disabledColor;
      }
      else if (_isSelected) {
        _background.color = _selectedColor;
      }
      else {
        _background.color = _normalColor;
      }
    }
  }
}
