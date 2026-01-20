using Content.Scripts.Game.Craft;
using UnityEngine;

namespace Content.Scripts.Ui.Layers.WorldSpaceUI {
  public class ProgressBarWorldSpaceWidget : BaseWorldSpaceWidget {
    [SerializeField] private UnityEngine.UI.Image _progressBarImage;
    private IProgressProvider _progression;

    public void SetProgress(float progress) {
      if (_progressBarImage != null) {
        _progressBarImage.fillAmount = Mathf.Clamp01(progress);
      }
    }

    public override void Repaint() {
      if (_progression == null) return;
      SetProgress(_progression.progress);
    }

    public override void SetTarget(Transform target) {
      base.SetTarget(target);
      _progression = _target.GetComponent<IProgressProvider>();
      if (_progression == null) {
        Debug.LogError($"Failed to get IProgressProvider from target {_target.name}", _target);
      }
    }
  }
}