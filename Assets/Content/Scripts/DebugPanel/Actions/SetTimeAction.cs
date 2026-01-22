using Content.Scripts.Core.Simulation;
using UnityEngine;

namespace Content.Scripts.DebugPanel.Actions {
  public class SetTimeAction : IDebugAction {
    private readonly SimulationTimeController _simTime;
    private readonly float _targetHour;
    private readonly string _displayName;

    public SetTimeAction(SimulationTimeController simTime, float targetHour, string displayName) {
      _simTime = simTime;
      _targetHour = targetHour;
      _displayName = displayName;
    }

    public string displayName => _displayName;
    public DebugCategory category => DebugCategory.Environment;
    public DebugActionType actionType => DebugActionType.Instant;

    public void Execute(DebugActionContext context) {
      // Assuming SimulationTimeController has a method to set time
      // If not - need to add SetTime(float hours) method
      Debug.Log($"[DebugAction] Set time to {_targetHour}:00 ({_displayName})");
      // TODO: _simTime.SetTime(_targetHour);
      Debug.LogWarning("[DebugAction] SetTime not implemented in SimulationTimeController yet");
    }
  }
}

