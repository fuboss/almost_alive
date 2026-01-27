using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public class TabButton : MonoBehaviour {
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Image _background;

    [Header("Colors")]
    [SerializeField] private Color _selectedColor = new(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color _normalColor = new(0.1f, 0.1f, 0.1f, 1f);

    private IInfoPanel _infoPanel;
    private Action<IInfoPanel> _onSelect;
    private bool _isSelected;

    public IInfoPanel infoPanel => _infoPanel;

    public bool isSelected {
      get => _isSelected;
      set {
        _isSelected = value;
        _infoPanel.active = _isSelected;
        UpdateVisuals();
      }
    }

    public void Setup(AgentInspectorView view, IInfoPanel panel) {
      _infoPanel = panel;
      _onSelect = view.SelectTab;
      _titleText.text = panel.tab;
      _button.onClick.RemoveAllListeners();
      _button.onClick.AddListener(OnClick);
    }

    // Legacy support
    public void Setup(MainInfoPanel mainInfoPanel, IInfoPanel panel) {
      _infoPanel = panel;
      _onSelect = mainInfoPanel.SelectTab;
      _titleText.text = panel.tab;
      _button.onClick.RemoveAllListeners();
      _button.onClick.AddListener(OnClick);
    }

    private void OnClick() {
      _onSelect?.Invoke(_infoPanel);
    }

    private void UpdateVisuals() {
      if (_background != null)
        _background.color = _isSelected ? _selectedColor : _normalColor;
    }
  }
}
