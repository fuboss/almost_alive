using UnityEngine;

namespace Content.Scripts.Ui {
  [RequireComponent(typeof(Canvas))]
  public class UILayer : MonoBehaviour {
    [SerializeField] private int _sortingOrder;
    [SerializeField] private CanvasGroup _mainCanvasGroup;

    public virtual int sortingOrder {
      get => _sortingOrder;
      set => _sortingOrder = value;
    }

    public float alpha {
      get => _mainCanvasGroup.alpha;
      set => _mainCanvasGroup.alpha = value;
    }

    public virtual void SetVisible(bool visible) {
      gameObject.SetActive(visible);
    }

    private void OnValidate() {
      _mainCanvasGroup = GetComponent<CanvasGroup>();
    }
    
    public virtual void Show() {
      SetVisible(true);
    }

    public virtual void Hide() {
      SetVisible(false);
    }
  }
}