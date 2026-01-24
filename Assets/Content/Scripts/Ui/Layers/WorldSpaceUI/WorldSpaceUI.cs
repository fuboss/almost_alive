using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Game.Craft;
using Content.Scripts.Game.Trees;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Content.Scripts.Ui.Layers.WorldSpaceUI {
  public interface IWorldSpaceWidget {
    public Transform target { get; }
    public RectTransform rect { get; }
    public bool isVisible { get; set; }

    public void Repaint();
  }

  [Serializable]
  public class WorldSpaceWidgets {
    public ProgressBarWorldSpaceWidget progressBarWidget;
    public LabelWorldSpaceWidget labelWidget;
  }

  public class WorldSpaceUI : UILayer {
    [SerializeField] private RectTransform _container;
    [SerializeField] private float _yPadding = 0.2f;
    [ShowInInspector] private List<IWorldSpaceWidget> _widgets = new();

    [SerializeField] private WorldSpaceWidgets _widgetsPrefabs;

    public void RegisterWidget(IWorldSpaceWidget widget) {
      if (_widgets.Contains(widget)) return;
      widget.rect.SetParent(_container);
      _widgets.Add(widget);
    }

    public void UnregisterWidgetsWithActor(UnfinishedActorBase actor) {
      foreach (var widget in _widgets.Where(w => w.target == actor.transform).ToArray()) {
        UnregisterWidget(widget);
      }
    }

    public void UnregisterWidget(IWorldSpaceWidget widget) {
      if (!_widgets.Contains(widget)) return;
      _widgets.Remove(widget);
      if (widget.rect != null) {
        Destroy(widget.rect.gameObject);
      }
    }

    public IWorldSpaceWidget CreateProgressBar(IProgressProvider actor) {
      var widget = Instantiate(_widgetsPrefabs.progressBarWidget, _container);
      widget.SetTarget(actor.actor.transform);
      widget.Repaint();
      RegisterWidget(widget);
      return widget;
    }
    
    public void UnregisterWidgetsWithTarget(Transform target) {
      foreach (var widget in _widgets.Where(w => w.target == target).ToArray()) {
        UnregisterWidget(widget);
      }
    }

    public override void OnUpdate() {
      base.OnUpdate();
      if (_container == null) return;
      if (_widgets == null || _widgets.Count == 0) return;

      UpdateWidgets();
    }

    private void UpdateWidgets() {
      var toDelete = new List<IWorldSpaceWidget>();
      foreach (var worldSpaceWidget in _widgets) {
        var target = worldSpaceWidget.target;
        if (target == null) {
          toDelete.Add(worldSpaceWidget);
          continue;
        }

        var visible = UIHelper.IsWorldPosOnScreen(Camera.main, target.position);
        worldSpaceWidget.isVisible = visible;
        if (!visible) continue;

        var screenPoint = GetWidgetPosition(target);
        worldSpaceWidget.rect.anchoredPosition = screenPoint;
        worldSpaceWidget.Repaint();
      }

      foreach (var toDel in toDelete) {
        UnregisterWidget(toDel);
      }
    }

    private Vector2 GetWidgetPosition(Transform t) {
      var position = t.position;
      var widgetPosition = UIHelper.WorldToCanvasPosition(_container, Camera.main,
        new Vector3(position.x, position.y + _yPadding, position.z));
      return widgetPosition;
    }
  }
}