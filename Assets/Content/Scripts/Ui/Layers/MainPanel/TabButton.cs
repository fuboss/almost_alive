using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public class TabButton : MonoBehaviour {
    public Image iconImage;
    public Button button;
    public TMP_Text titleText;

    [SerializeField] private bool _isSelected;
    private MainInfoPanel _mainInfoPanel;
    private IInfoPanel _infoPanel;

    public bool isSelected {
      get => _isSelected;
      set {
        _isSelected = value;
        infoPanel.active = _isSelected;
      }
    }

    public IInfoPanel infoPanel => _infoPanel;


    public void Setup(MainInfoPanel mainInfoPanel, IInfoPanel infoPanel) {
      _infoPanel = infoPanel;
      _mainInfoPanel = mainInfoPanel;
      titleText.text = infoPanel.tab;
      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(OnClick);
    }

    private void OnClick() {
      _mainInfoPanel.SelectTab(_infoPanel);
    }
  }
}