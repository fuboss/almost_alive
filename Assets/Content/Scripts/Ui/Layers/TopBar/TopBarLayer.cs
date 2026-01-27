using Content.Scripts.Core.Environment;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Ui.Layers.ControlsPanel;
using TMPro;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Ui.Layers.TopBar {
  /// <summary>
  /// Top bar HUD layer. Contains time, resources, camera mode.
  /// </summary>
  public class TopBarLayer : UILayer {
    [Header("Widgets")]
    [SerializeField] private TMP_Text _timeText;
    [SerializeField] private CameraModeWidget _cameraModeWidget;
    [SerializeField] private ResourcePanelWidget _resourcePanel;
    [SerializeField] private SimSpeedWidget _simSpeedWidget;

    [Header("Time Settings")]
    [SerializeField] private float _secondsPerGameHour = 60f;

    [Inject] private SimulationTimeController _simTime;
    [Inject] private readonly EnvironmentSetupSO _setup;

    public override void Initialize() {
      base.Initialize();
      Show();
      _simSpeedWidget.Init(_simTime);
      _secondsPerGameHour = _setup.dayLengthSeconds / 24f;
    }

    public override void OnUpdate() {
      base.OnUpdate();
      UpdateTimeDisplay();
    }

    private void UpdateTimeDisplay() {
      if (_timeText == null || _simTime == null) return;

      var totalHours = _simTime.totalSimTime / _secondsPerGameHour;
      var day = (int)(totalHours / 24) + 1;
      var hour = (int)(totalHours % 24);
      var minute = (int)((totalHours * 60) % 60);

      _timeText.text = $"Day {day}, {hour:D2}:{minute:D2}";
    }
  }
}
