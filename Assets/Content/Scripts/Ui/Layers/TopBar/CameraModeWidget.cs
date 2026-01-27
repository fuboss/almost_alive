using Content.Scripts.Game.Camera;
using Content.Scripts.Game.Interaction;
using Content.Scripts.Ui.Services;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Content.Scripts.Ui.Layers.TopBar {
  /// <summary>
  /// Camera mode toggle: Free / Follow.
  /// </summary>
  public class CameraModeWidget : MonoBehaviour {
    [SerializeField] private Button _freeButton;
    [SerializeField] private Button _followButton;
    [SerializeField] private Color _activeColor = new(0.36f, 0.54f, 0.36f, 1f);
    [SerializeField] private Color _inactiveColor = new(0.25f, 0.25f, 0.25f, 1f);

    [Inject] private CameraModule _camera;
    [Inject] private SelectionService _selection;

    private bool _isFollowMode;

    public void Init() {
      _freeButton.onClick.AddListener(OnFreeClicked);
      _followButton.onClick.AddListener(OnFollowClicked);
      _selection.OnSelected += OnSelectionChanged;
      UpdateButtons();
    }

    private void OnDestroy() {
      if (_selection != null)
        _selection.OnSelected -= OnSelectionChanged;
    }

    private void OnFreeClicked() {
      _isFollowMode = false;
      _camera.DisableFollowMode();
      UpdateButtons();
    }

    private void OnFollowClicked() {
      var selected = _selection.current;
      if (selected == null) return;

      _isFollowMode = true;
      _camera.AddToTargetGroup(selected.gameObject.transform);
      _camera.EnableFollowMode();
      UpdateButtons();
    }

    private void OnSelectionChanged(ISelectableActor current, ISelectableActor prev) {
      // Remove prev from target group if was following
      if (prev != null && _isFollowMode) {
        _camera.RemoveFromTargetGroup(prev.gameObject.transform);
      }

      // Add new to target group if in follow mode
      if (current != null && _isFollowMode) {
        _camera.AddToTargetGroup(current.gameObject.transform);
      }
      else if (current == null) {
        _isFollowMode = false;
        _camera.DisableFollowMode();
        UpdateButtons();
      }
    }

    private void UpdateButtons() {
      SetButtonActive(_freeButton, !_isFollowMode);
      SetButtonActive(_followButton, _isFollowMode);
    }

    private void SetButtonActive(Button btn, bool active) {
      var colors = btn.colors;
      colors.normalColor = active ? _activeColor : _inactiveColor;
      btn.colors = colors;
    }
  }
}
