using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Content.Scripts.Game.Camera.Input {
  /// <summary>
  /// Handles all camera-related input using the new Input System
  /// </summary>
  public class CameraInputHandler : IDisposable {
    private readonly InputAction _moveAction;
    private readonly InputAction _zoomAction;
    private readonly InputAction _rotateAction;
    private readonly InputAction _rotateLeftAction;
    private readonly InputAction _rotateRightAction;
    private readonly InputAction _middleMouseAction;
    private readonly InputAction _mouseDeltaAction;
    private readonly InputAction _mousePositionAction;

    private bool _isMiddleMouseHeld;

    public Vector2 MoveInput => _moveAction.ReadValue<Vector2>();
    public float ZoomInput => _zoomAction.ReadValue<Vector2>().y;
    public float DiscreteRotation { get; private set; }
    public Vector2 MouseDelta => _isMiddleMouseHeld ? _mouseDeltaAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 MouseScreenPosition => _mousePositionAction.ReadValue<Vector2>();
    public bool IsMiddleMouseHeld => _isMiddleMouseHeld;

    public CameraInputHandler() {
      // WASD / Arrow keys movement
      _moveAction = new InputAction("CameraMove", InputActionType.Value);
      _moveAction.AddCompositeBinding("2DVector")
        .With("Up", "<Keyboard>/w")
        .With("Down", "<Keyboard>/s")
        .With("Left", "<Keyboard>/a")
        .With("Right", "<Keyboard>/d");
      _moveAction.AddCompositeBinding("2DVector")
        .With("Up", "<Keyboard>/upArrow")
        .With("Down", "<Keyboard>/downArrow")
        .With("Left", "<Keyboard>/leftArrow")
        .With("Right", "<Keyboard>/rightArrow");

      // Mouse scroll zoom
      _zoomAction = new InputAction("CameraZoom", InputActionType.Value, "<Mouse>/scroll");

      // Q/E discrete rotation
      _rotateLeftAction = new InputAction("RotateLeft", InputActionType.Button, "<Keyboard>/q");
      _rotateRightAction = new InputAction("RotateRight", InputActionType.Button, "<Keyboard>/e");

      // Middle mouse for free rotation
      _middleMouseAction = new InputAction("MiddleMouse", InputActionType.Button, "<Mouse>/middleButton");
      _mouseDeltaAction = new InputAction("MouseDelta", InputActionType.Value, "<Mouse>/delta");
      _mousePositionAction = new InputAction("MousePosition", InputActionType.Value, "<Mouse>/position");

      // Subscribe to discrete rotation
      _rotateLeftAction.performed += _ => DiscreteRotation = -1f;
      _rotateRightAction.performed += _ => DiscreteRotation = 1f;

      // Middle mouse state
      _middleMouseAction.performed += _ => _isMiddleMouseHeld = true;
      _middleMouseAction.canceled += _ => _isMiddleMouseHeld = false;

      EnableAll();
    }

    /// <summary>
    /// Consume the discrete rotation input (call after processing)
    /// </summary>
    public void ConsumeDiscreteRotation() {
      DiscreteRotation = 0f;
    }

    public void EnableAll() {
      _moveAction.Enable();
      _zoomAction.Enable();
      _rotateLeftAction.Enable();
      _rotateRightAction.Enable();
      _middleMouseAction.Enable();
      _mouseDeltaAction.Enable();
      _mousePositionAction.Enable();
    }

    public void DisableAll() {
      _moveAction.Disable();
      _zoomAction.Disable();
      _rotateLeftAction.Disable();
      _rotateRightAction.Disable();
      _middleMouseAction.Disable();
      _mouseDeltaAction.Disable();
      _mousePositionAction.Disable();
    }

    public void Dispose() {
      _moveAction?.Dispose();
      _zoomAction?.Dispose();
      _rotateLeftAction?.Dispose();
      _rotateRightAction?.Dispose();
      _middleMouseAction?.Dispose();
      _mouseDeltaAction?.Dispose();
      _mousePositionAction?.Dispose();
    }
  }
}

