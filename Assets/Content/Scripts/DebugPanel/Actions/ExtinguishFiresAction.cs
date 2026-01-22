using Content.Scripts.World.Vegetation;
using UnityEngine;

namespace Content.Scripts.DebugPanel.Actions {
  public class ExtinguishFiresAction : IDebugAction {
    public string displayName => "Extinguish All Fires";
    public DebugCategory category => DebugCategory.Environment;
    public DebugActionType actionType => DebugActionType.Instant;

    public void Execute(DebugActionContext context) {
      if (VegetationManager.Instance != null && VegetationManager.Instance.IsInitialized) {
        VegetationManager.Instance.ExtinguishAll();
        Debug.Log("[DebugAction] Extinguished all fires");
      } else {
        Debug.LogWarning("[DebugAction] VegetationManager not available");
      }
    }
  }
}


