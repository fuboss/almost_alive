using Content.Scripts.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Content.Scripts.DebugPanel {
  public class DebugPanelLayer : UILayer {
    [Inject] private DebugModule _debugModule;

    private DebugPanelUI _panelUI;
    private Canvas _canvas;
    public static DebugPanelLayer Instance { get; private set; }

    public override void Initialize() {
      base.Initialize();

      _canvas = GetComponent<Canvas>();
      if (_canvas == null) {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000; // on top of all other UI
      }

      // Add CanvasScaler for proper scaling
      var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
      if (scaler == null) {
        scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
      }

      // Add GraphicRaycaster for interaction
      if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null) {
        gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
      }

      // Create UI
      _panelUI = gameObject.AddComponent<DebugPanelUI>();
      _panelUI.Initialize(_debugModule);

      // Subscribe to state changes
      _debugModule.OnStateChanged += OnDebugStateChanged;
      Instance = this;
    }


    private void OnDebugStateChanged(DebugState newState) {
      if (newState == DebugState.Idle) {
        Hide();
      }
      else if (newState == DebugState.Browsing) {
        Show();
      }
    }

    private void OnDestroy() {
      if (_debugModule != null) {
        _debugModule.OnStateChanged -= OnDebugStateChanged;
      }

      Instance = this;
    }
  }
}