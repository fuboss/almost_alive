using System.Collections.Generic;
using System.Linq;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Ui {
  public class UiModule : ITickable {
    [Inject] private IEnumerable<UILayer> _uiLayers;

    public void SetLayers(params UILayer[] targetLayers) {
      if (targetLayers.Length == 0) {
        HideAllLayers();
        return;
      }

      foreach (var layer in _uiLayers) {
        var shouldBeVisible = targetLayers.Contains(layer);
        if (layer.isVisible == shouldBeVisible) continue;

        if (!shouldBeVisible) {
          layer.Hide();
        }
        else {
          layer.Show();
        }
      }
    }


    public T GetLayer<T>() where T : UILayer {
      foreach (var layer in _uiLayers) {
        if (layer is T tLayer) {
          return tLayer;
        }
      }

      return null;
    }

    public void HideAllLayers() {
      foreach (var layer in _uiLayers) {
        if (layer.isVisible) {
          layer.Hide();
        }
      }
    }

    public void Tick() {
      foreach (var uiLayer in _uiLayers) {
        if (uiLayer.isVisible) {
          uiLayer.OnUpdate();
        }
      }
    }

    public void RemoveLayer(UILayer layer) {
      if (layer != null && layer.isVisible) {
        layer.Hide();
      }
    }

    public void AddLayer(UILayer layer) {
      if (layer != null && !layer.isVisible) {
        layer.Show();
      }
    }
  }
}