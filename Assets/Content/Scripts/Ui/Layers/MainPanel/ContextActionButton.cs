using Content.Scripts.Game.Interaction;
using Content.Scripts.Ui.Layers.Inspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.MainPanel {
  /// <summary>
  /// Button for context action in inspector.
  /// </summary>
  public class ContextActionButton : MonoBehaviour {
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _labelText;
    [SerializeField] private TMP_Text _iconText;

    private IContextAction _action;
    private ISelectableActor _target;

    public void Setup(IContextAction action, ISelectableActor target) {
      _action = action;
      _target = target;

      _labelText.text = action.label;
      if (_iconText != null) _iconText.text = action.icon;

      _button.onClick.RemoveAllListeners();
      _button.onClick.AddListener(OnClick);
    }

    private void OnClick() {
      if (_action != null && _action.CanExecute(_target)) {
        _action.Execute(_target);
      }
    }
  }
}
