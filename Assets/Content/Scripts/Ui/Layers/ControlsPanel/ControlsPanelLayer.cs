using Content.Scripts.Core.Simulation;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Ui.Layers.ControlsPanel {
  public class ControlsPanelLayer : UILayer {
    [SerializeField] private SimSpeedWidget _simSpeedWidget;
    [Inject] private SimulationTimeController _simTime;
    public override void Initialize() {
      base.Initialize();

      _simSpeedWidget.Init(_simTime);
      Show(); // HUD — always visible
    }
  }
}
