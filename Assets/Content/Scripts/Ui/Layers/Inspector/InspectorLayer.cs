using System.Collections.Generic;
using Content.Scripts.Game.Interaction;
using Content.Scripts.Ui.Services;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Ui.Layers.Inspector {
  /// <summary>
  /// Universal inspector layer. Switches between views based on selected actor type.
  /// Views are checked in order - first matching view wins.
  /// </summary>
  public class InspectorLayer : UILayer {
    [SerializeField] private List<IInspectorView> _viewComponents = new();
    [SerializeField] private GameObject _emptyStatePlaceholder;

    [Inject] private SelectionService _selection;

    private readonly List<IInspectorView> _views = new();
    private IInspectorView _activeView;

    public override void Initialize() {
      base.Initialize();

      foreach (var component in _viewComponents) {
        _views.Add(component);
        component.Hide();
      }

      _selection.OnSelected += OnSelectionChanged;
    }

    public override void Show() {
      base.Show();
      ShowEmptyState();
    }

    private void OnDestroy() {
      if (_selection != null)
        _selection.OnSelected -= OnSelectionChanged;
    }

    private void OnSelectionChanged(ISelectableActor current, ISelectableActor prev) {
      if (current == null) {
        HideActiveView();
        ShowEmptyState();
        return;
      }

      // Find matching view
      foreach (var view in _views) {
        if (view.CanHandle(current)) {
          SwitchToView(view, current);
          return;
        }
      }

      // No matching view
      HideActiveView();
      ShowEmptyState();
    }

    private void SwitchToView(IInspectorView view, ISelectableActor actor) {
      HideActiveView();
      HideEmptyState();
      
      _activeView = view;
      _activeView.Show(actor);
    }

    private void HideActiveView() {
      _activeView?.Hide();
      _activeView = null;
    }

    private void ShowEmptyState() {
      if (_emptyStatePlaceholder != null)
        _emptyStatePlaceholder.SetActive(true);
    }

    private void HideEmptyState() {
      if (_emptyStatePlaceholder != null)
        _emptyStatePlaceholder.SetActive(false);
    }

    public override void OnUpdate() {
      base.OnUpdate();
      _activeView?.OnUpdate();
    }
  }
}
