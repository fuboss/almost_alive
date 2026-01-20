using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Ui.Layers.WorldSpaceUI {
  public abstract class BaseWorldSpaceWidget : SerializedMonoBehaviour, IWorldSpaceWidget {
    [SerializeField] protected Transform _target;
    [SerializeField] protected RectTransform _rect;
    [ShowInInspector] protected bool _isVisible;

    public Transform target => _target;
    public RectTransform rect => _rect;

    public virtual bool isVisible {
      get => _isVisible;
      set {
        if (_isVisible == value) return;
        _isVisible = value;
        gameObject.SetActive(_isVisible);
      }
    }

    public virtual void Repaint() {
    }

    public virtual void SetTarget(Transform target) {
      _target = target;
      _isVisible = gameObject.activeSelf;
    }
  }
}