using Content.Scripts.Game;
using Content.Scripts.Game.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Ui.Services {
  /// <summary>
  /// Handles mouse click to select any ISelectableActor.
  /// </summary>
  public class SelectionInputHandler : ITickable {
    [Inject] private SelectionService _selection;

    private const float MAX_RAYCAST_DISTANCE = 100f;
    private Camera _camera;

    public void Tick() {
      if (!Mouse.current.leftButton.wasPressedThisFrame) return;
      if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

      _camera ??= Camera.main;
      if (_camera == null) return;

      var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
      if (!Physics.Raycast(ray, out var hit, MAX_RAYCAST_DISTANCE)) {
        _selection.ClearSelection();
        return;
      }

      // Try find any ISelectableActor in hierarchy
      var selectable = hit.collider.GetComponentInParent<ISelectableActor>();
      if (selectable != null && selectable.canSelect) {
        _selection.Select(selectable);
      }
      else {
        _selection.ClearSelection();
      }
    }
  }
}
