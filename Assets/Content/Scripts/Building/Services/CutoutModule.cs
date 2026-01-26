using System.Collections.Generic;
using Content.Scripts.Building.Runtime;
using Content.Scripts.Game.Camera;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Building.Services {
  /// <summary>
  /// Centralized cutout management via camera spherecast.
  /// Detects obstructions between camera and focus point, applies cutout.
  /// </summary>
  public class CutoutModule : ILateTickable, IInitializable {
    [Inject] private CameraModule _cameraModule;
    private Camera _mainCamera;

    private readonly HashSet<StructureCutoutController> _activeControllers = new();
    private readonly Dictionary<GameObject, StructureCutoutController> _controllerCache = new();

    private float _spherecastRadius = 1.5f;
    private float _maxDistance = 30f;
    private int _obstructionLayer;
    private float _checkInterval = 0.1f;
    private float _nextCheckTime;
    private readonly RaycastHit[] _castBuffer = new RaycastHit[24];

    void IInitializable.Initialize() {
      _obstructionLayer = LayerMask.GetMask("Default", "Actor"); // adjust as needed
      _mainCamera = Camera.main;
    }

    void ILateTickable.LateTick() {
      if (Time.time < _nextCheckTime) return;
      _nextCheckTime = Time.time + _checkInterval;

      UpdateCutout();
    }

    private void UpdateCutout() {
      var cameraState = _cameraModule.GetState();
      var cameraPos = _mainCamera.transform.position;
      var focusPoint = cameraState.FocusPoint;

      // Use camera forward as direction to cast (where camera is looking)
      var cameraForward = _mainCamera.transform.forward;
      var distanceToFocus = Vector3.Distance(cameraPos, focusPoint);
      var drawDistance = Mathf.Min(distanceToFocus, _maxDistance);

      var newActive = new HashSet<StructureCutoutController>();
      // Spherecast from camera in the camera's forward direction
      var size = Physics.SphereCastNonAlloc(cameraPos, _spherecastRadius, cameraForward, _castBuffer,
        drawDistance, _obstructionLayer);

      // gather hit points for gizmo
      for (int i = 0; i < size; i++) {
        var hit = _castBuffer[i];
        var controller = GetOrCacheController(hit.collider.gameObject);
        if (controller == null) continue;
        newActive.Add(controller);

        // Update cutout center to hit point
        controller.SetCutoutCenter(hit.point);
        controller.SetCutoutRadius(3f); // configurable

        if (!_activeControllers.Contains(controller)) {
          controller.EnableCutout();
        }
      }

      // Disable controllers that are no longer hit
      foreach (var controller in _activeControllers) {
        if (!newActive.Contains(controller)) {
          controller.DisableCutout();
        }
      }

      _activeControllers.Clear();
      foreach (var c in newActive) _activeControllers.Add(c);
    }

    private StructureCutoutController GetOrCacheController(GameObject go) {
      if (_controllerCache.TryGetValue(go, out var cached)) return cached;

      // Try hierarchy (object or parents)
      var controller = go.GetComponentInParent<StructureCutoutController>();

      if (controller != null) {
        _controllerCache[go] = controller;
      }

      return controller;
    }

    public void SetSphereCastRadius(float radius) => _spherecastRadius = radius;
    public void SetCheckInterval(float interval) => _checkInterval = interval;
  }
}