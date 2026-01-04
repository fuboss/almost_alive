using System.Linq;
using Content.Scripts.Core.Loop;
using Reflex.Attributes;

namespace Content.Scripts.Ui {
  public class UiModule : IUpdatable {
    [Inject] private UILayer[] _uiLayers;

    public void SetVisibleLayers(params UILayer[] targetLayers) {
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
        layer.Hide();
      }
    }

    public void OnUpdate() {
      foreach (var uiLayer in _uiLayers) {
        if (uiLayer.isVisible) {
          uiLayer.OnUpdate();
        }
      }
    }
  }
}