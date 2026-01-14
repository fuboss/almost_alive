using Content.Scripts.Ui.Layers.ControlsPanel;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Ui {
  public class GameUIModule : IStartable, ITickable, IInitializable {
    [Inject] private UiModule _uiModule;
    [Inject] private ControlsPanelLayer _controlsPanelLayer;
    
    public void Initialize() {
    }

    public void Start() {
      _uiModule.AddLayer(_controlsPanelLayer);
    }

    public void Tick() {
    }
  }
}