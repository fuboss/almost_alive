using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.MainPanel {
  /// <summary>
  /// Single action item in the plan list.
  /// </summary>
  public class PlanActionItem : MonoBehaviour {
    [SerializeField] private TMP_Text _indexText;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Image _activeIndicator;
    [SerializeField] private Image _background;

    [Header("Colors")]
    [SerializeField] private Color _activeColor = new(0.36f, 0.54f, 0.36f, 0.3f);
    [SerializeField] private Color _inactiveColor = new(0f, 0f, 0f, 0f);
    [SerializeField] private Color _activeTextColor = new(0.83f, 0.83f, 0.83f, 1f);
    [SerializeField] private Color _inactiveTextColor = new(0.53f, 0.53f, 0.53f, 1f);

    public void Setup(int index, string actionName, float cost, bool isActive) {
      _indexText.text = $"{index}.";
      _nameText.text = actionName;
      SetActive(isActive);
    }

    private void SetActive(bool isActive) {
      if (_background != null)
        _background.color = isActive ? _activeColor : _inactiveColor;

      if (_activeIndicator != null)
        _activeIndicator.gameObject.SetActive(isActive);

      _nameText.color = isActive ? _activeTextColor : _inactiveTextColor;
      _indexText.color = isActive ? _activeTextColor : _inactiveTextColor;
    }
  }
}
