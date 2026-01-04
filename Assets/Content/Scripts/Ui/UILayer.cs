using Sirenix.OdinInspector;
using UnityEngine;
using VContainer.Unity;

namespace Content.Scripts.Ui {
  [RequireComponent(typeof(Canvas))]
  public class UILayer : SerializedMonoBehaviour, IInitializable {
    [SerializeField] private int _sortingOrder;
    [SerializeField] private CanvasGroup _mainCanvasGroup;
    public virtual bool isVisible { get; protected set; }

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
      isVisible = visible;
    }

    private void OnValidate() {
      _mainCanvasGroup = GetComponent<CanvasGroup>();
    }

    public virtual void Show() {
      if (isVisible) return;
      SetVisible(true);
    }

    public virtual void Hide() {
      if (!isVisible) return;
      SetVisible(false);
    }

    public virtual void Initialize() {
      Debug.LogError($"[UI][{GetType().Name}] Initialize()", this);
      isVisible = gameObject.activeSelf;
      Hide();
    }

    public virtual void OnUpdate() {
    }
  }
}