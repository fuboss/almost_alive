using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Game.Camera.Components;
using Content.Scripts.Game.Camera.Input;
using Content.Scripts.Game.Camera.Settings;
using Content.Scripts.Game.Camera.Strategies;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Game.Camera {
  /// <summary>
  /// Main camera orchestrator module - coordinates all camera subsystems
  /// </summary>
  public class CameraModule : IInitializable, ILateTickable, IStartable, IDisposable {
    [Inject] private CinemachineCamera _camera;
    [Inject] private CinemachineTargetGroup _targetGroup;
    [Inject] private CameraSettingsSO _settings;

    private CameraState _state;

    private CameraInputHandler _inputHandler;

    private CameraZoomController _zoomController;
    private CameraRotationController _rotationController;

    private List<ICameraMovementStrategy> _movementStrategies;
    private ICameraMovementStrategy _activeStrategy;

    private Transform _cameraRig;

    void IInitializable.Initialize() {
      _state = new CameraState();
      _inputHandler = new CameraInputHandler();

      _zoomController = new CameraZoomController(_settings, _inputHandler, _state);
      _rotationController = new CameraRotationController(_settings, _inputHandler, _state);

      InitializeStrategies();

      SetupCameraRig();
    }

    private void InitializeStrategies() {
      _movementStrategies = new List<ICameraMovementStrategy> {
        new FreeCameraMovement(_settings, _inputHandler, _state),
        new FocusOnPointMovement(_settings),
        new FollowTargetMovement(_settings, _targetGroup)
      };

      _movementStrategies = _movementStrategies.OrderByDescending(s => s.priority).ToList();
    }

    private void SetupCameraRig() {
      _cameraRig = new GameObject("CameraRig").transform;
      _state.FocusPoint = _targetGroup != null ? _targetGroup.transform.position : Vector3.zero;
      _cameraRig.position = _state.FocusPoint;

      DisableCinemachineAutoPositioning();

      _camera.Target = new CameraTarget {
        CustomLookAtTarget = false,
        TrackingTarget = _cameraRig,
        LookAtTarget = _cameraRig
      };

      _zoomController.SetZoom(0.5f, immediate: true);
    }

    private void DisableCinemachineAutoPositioning() {
      var positionComposer = _camera.GetComponent<CinemachinePositionComposer>();
      if (positionComposer != null) {
        positionComposer.enabled = false;
      }

      var rotationComposer = _camera.GetComponent<CinemachineRotationComposer>();
      if (rotationComposer != null) {
        rotationComposer.enabled = false;
      }

      var follow = _camera.GetComponent<CinemachineFollow>();
      if (follow != null) {
        follow.enabled = false;
      }
    }

    void IStartable.Start() {
      UpdateCameraTransform();
      
      if (_settings.showDebugGizmos) {
        var debugVis = _camera.gameObject.AddComponent<CameraDebugVisualizer>();
        debugVis.state = _state;
      }
    }

    void ILateTickable.LateTick() {
      var deltaTime = Time.deltaTime;

      _zoomController.Update(deltaTime);
      _rotationController.Update(deltaTime);

      UpdateMovementStrategy(deltaTime);

      UpdateCameraTransform();
    }

    private void UpdateMovementStrategy(float deltaTime) {
      var newStrategy = _movementStrategies.FirstOrDefault(s => s.isActive);

      if (newStrategy != _activeStrategy) {
        _activeStrategy?.OnDeactivate();
        _activeStrategy = newStrategy;
        _activeStrategy?.OnActivate();
        _state.IsFollowMode = _activeStrategy is FollowTargetMovement;
      }

      if (_activeStrategy != null) {
        _state.FocusPoint = _activeStrategy.UpdateFocusPoint(_state.FocusPoint, deltaTime);
      }
    }

    private void UpdateCameraTransform() {
      _cameraRig.position = _state.FocusPoint;

      var groundY = GetGroundHeight(_state.FocusPoint);
      var focusPointOnGround = new Vector3(_state.FocusPoint.x, groundY, _state.FocusPoint.z);

      var pitchRad = _state.CurrentPitch * Mathf.Deg2Rad;
      var horizontalDistance = _state.CurrentHeight / Mathf.Tan(pitchRad);
      var yawRotation = Quaternion.Euler(0f, _state.CurrentYaw, 0f);
      var offsetDirection = yawRotation * Vector3.back;
      var cameraPosition = focusPointOnGround 
                           + offsetDirection * horizontalDistance 
                           + Vector3.up * _state.CurrentHeight;

      var lookDirection = (focusPointOnGround - cameraPosition).normalized;
      var lookRotation = Quaternion.LookRotation(lookDirection);

      _camera.transform.SetPositionAndRotation(cameraPosition, lookRotation);
    }

    private float GetGroundHeight(Vector3 position) {
      var rayOrigin = new Vector3(position.x, position.y + 500f, position.z);
      if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, 1000f, this._settings.groundCheckLayer, QueryTriggerInteraction.Ignore)) {
        return hit.point.y;
      }
      return 0f;
    }

    #region Public API


    public void AddToTargetGroup(Transform target, float weight = 1f, float radius = 5f) {
      _targetGroup.Targets ??= new List<CinemachineTargetGroup.Target>();
      _targetGroup.Targets.Add(new CinemachineTargetGroup.Target {
        Object = target,
        Weight = weight,
        Radius = radius
      });
    }


    public void RemoveFromTargetGroup(Transform target) {
      if (_targetGroup.Targets == null) return;
      
      var index = _targetGroup.Targets.FindIndex(t => t.Object == target);
      if (index >= 0) {
        _targetGroup.Targets.RemoveAt(index);
      }
    }


    public void EnableFollowMode() {
      var followStrategy = _movementStrategies.OfType<FollowTargetMovement>().FirstOrDefault();
      followStrategy?.SetActive(true);
    }


    public void DisableFollowMode() {
      var followStrategy = _movementStrategies.OfType<FollowTargetMovement>().FirstOrDefault();
      followStrategy?.SetActive(false);
    }


    public void FocusOn(Vector3 worldPosition) {
      DisableFollowMode();
      var focusStrategy = _movementStrategies.OfType<FocusOnPointMovement>().FirstOrDefault();
      focusStrategy?.FocusOn(worldPosition);
    }


    public void FocusOnImmediate(Vector3 worldPosition) {
      DisableFollowMode();
      _state.FocusPoint = worldPosition;
    }

    public void SetZoom(float normalizedZoom, bool immediate = false) {
      _zoomController.SetZoom(normalizedZoom, immediate);
    }


    public void SetYaw(float yaw, bool immediate = false) {
      _rotationController.SetYaw(yaw, immediate);
    }


    public CameraState GetState() => _state;

    #endregion

    void IDisposable.Dispose() {
      _inputHandler?.Dispose();
      
      if (_cameraRig != null) {
        UnityEngine.Object.Destroy(_cameraRig.gameObject);
      }
    }
  }
}