using Content.Scripts.AI.GOAP;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.Runtime;
using Content.Scripts.DebugPanel;
using Content.Scripts.DebugPanel.Actions;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.World.Grid.Presentation {
  /// <summary>
  /// Main service for WorldGrid visualization. Manages hover and footprint visualizers.
  /// </summary>
  public class WorldGridPresentationModule : IStartable, ILateTickable {
    [Inject] private readonly WorldGridPresentationConfigSO _config;
    [Inject] private readonly DebugModule _debugModule;
    [Inject] private readonly ActorCreationModule _actorCreationModule;

    private IHoverVisualizer _hoverVisualizer;
    private IFootprintVisualizer _footprintVisualizer;

    private GridVisualizationMode _currentMode = GridVisualizationMode.Hidden;
    private int _frameCounter;

    private Camera _mainCamera;

    // Raycast for ground detection
    private readonly RaycastHit[] _raycastBuffer = new RaycastHit[8];
    private LayerMask _groundLayerMask;
    private bool _layerMaskInitialized;

    public GridVisualizationMode currentMode => _currentMode;

    void IStartable.Start() {
      _mainCamera = Camera.main;
      if (_mainCamera == null) {
        Debug.LogError("[WorldGridPresentation] Camera.main is NULL!");
      }

      CreateVisualizers();

      // Subscribe to DebugModule events
      _debugModule.OnStateChanged += OnDebugStateChanged;

      Debug.Log("[WorldGridPresentation] Initialized");
    }

    void ILateTickable.LateTick() {
      _frameCounter++;

      if (_currentMode == GridVisualizationMode.PlacementPreview) {
        UpdatePlacementPreview();
      }
    }

    #region Public API

    public void SetMode(GridVisualizationMode mode) {
      if (_currentMode == mode) return;

      // Hide current
      if (_currentMode == GridVisualizationMode.PlacementPreview) {
        _hoverVisualizer?.Hide();
        _footprintVisualizer?.Hide();
      }

      _currentMode = mode;
      Debug.Log($"[WorldGridPresentation] Mode: {_currentMode}");
    }

    #endregion

    #region Visualizer Factory

    private void CreateVisualizers() {
      var container = new GameObject("WorldGridVisualizers");
      Object.DontDestroyOnLoad(container);

      // Create hover visualizer
      var hoverObj = new GameObject("HoverVisualizer");
      hoverObj.transform.SetParent(container.transform, false);
      var hover = hoverObj.AddComponent<HoverVisualizer>();
      hover.Initialize(_config);
      _hoverVisualizer = hover;

      // Create footprint visualizer
      var footprintObj = new GameObject("FootprintVisualizer");
      footprintObj.transform.SetParent(container.transform, false);
      var footprint = footprintObj.AddComponent<FootprintVisualizer>();
      footprint.Initialize(_config);
      _footprintVisualizer = footprint;

      Debug.Log("[WorldGridPresentation] Visualizers created");
    }

    #endregion

    #region Placement Preview

    private void UpdatePlacementPreview() {
      if (_frameCounter % _config.hoverUpdateInterval != 0) return;

      if (_debugModule.PendingAction == null) {
        _hoverVisualizer?.Hide();
        _footprintVisualizer?.Hide();
        return;
      }

      if (!TryGetGroundPosition(out Vector3 worldPos)) {
        _hoverVisualizer?.Hide();
        _footprintVisualizer?.Hide();
        return;
      }

      var groundCoord = GroundCoord.FromWorld(worldPos);
      var action = _debugModule.PendingAction;

      var isValid = ShowFootprint(action, out var showFootprint, out var showGround, out var footprint);

      if (showFootprint) {
        _footprintVisualizer.ShowFootprint(groundCoord, footprint, isValid);
        _hoverVisualizer?.Hide();
      }
      else if (showGround) {
        _footprintVisualizer?.Hide();
        _hoverVisualizer?.ShowHover(groundCoord, isValid);
      }
      else {
        _hoverVisualizer?.Hide();
        _footprintVisualizer?.Hide();
      }
    }

    private bool ShowFootprint(IDebugAction action, out bool showFootprint, out bool showGround,
      out Vector2Int footprint) {
      showFootprint = false;
      showGround = false;
      footprint = Vector2Int.zero;
      var isValid = true; // TODO: validation
      if (action is SpawnStructureAction structureAction) {
        footprint = structureAction.footprint;
        showFootprint = true;
      }
      else if (action.actionType != DebugActionType.Instant) {
        showGround = action.category is DebugCategory.Spawn or DebugCategory.Destroy or DebugCategory.Environment
          or DebugCategory.Events;
      }
      else if (action is SpawnActorAction spawnActorAction &&
               _actorCreationModule.TryGetComponentOnPrefab<Structure>(spawnActorAction.actorKey,
                 out var structureComp)) {
        footprint = structureComp.footprint;
        showFootprint = true;
      }

      return isValid;
    }

    private bool TryGetGroundPosition(out Vector3 worldPos) {
      worldPos = Vector3.zero;

      if (_mainCamera == null) return false;

      // Lazy init layer mask
      if (!_layerMaskInitialized) {
        _groundLayerMask = LayerMask.GetMask("Terrain", "Water", "Default");
        if (_groundLayerMask == 0) {
          _groundLayerMask = ~LayerMask.GetMask("UI", "Ignore Raycast");
        }

        _layerMaskInitialized = true;
      }

      var mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
      var ray = _mainCamera.ScreenPointToRay(mousePos);

      var hitCount = Physics.RaycastNonAlloc(ray, _raycastBuffer, 1000f, _groundLayerMask);

      if (hitCount == 0) return false;

      // Find closest hit (RaycastNonAlloc doesn't sort!)
      var closestDist = float.MaxValue;
      var closestIdx = -1;
      for (int i = 0; i < hitCount; i++) {
        if (_raycastBuffer[i].distance < closestDist) {
          closestDist = _raycastBuffer[i].distance;
          closestIdx = i;
        }
      }

      if (closestIdx < 0) return false;

      worldPos = _raycastBuffer[closestIdx].point;
      return true;
    }

    #endregion

    #region DebugModule Events

    private void OnDebugStateChanged(DebugState newState) {
      switch (newState) {
        case DebugState.Idle:
        case DebugState.Browsing:
          SetMode(GridVisualizationMode.Hidden);
          break;

        case DebugState.ReadyToApply:
          SetMode(GridVisualizationMode.PlacementPreview);
          break;
      }
    }

    #endregion
  }
}