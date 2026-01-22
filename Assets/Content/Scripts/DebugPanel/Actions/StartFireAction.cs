using Content.Scripts.World.Vegetation;
using UnityEngine;

namespace Content.Scripts.DebugPanel.Actions {
  public class StartFireAction : IDebugAction {
    public string displayName => "Start Fire";
    public DebugCategory category => DebugCategory.Environment;
    public DebugActionType actionType => DebugActionType.RequiresWorldPosition;

    public void Execute(DebugActionContext context) {
      if (VegetationManager.Instance != null && VegetationManager.Instance.IsInitialized) {
        VegetationManager.Instance.StartFireAt(context.worldPosition);
        Debug.Log($"[DebugAction] Started fire at {context.worldPosition}");
      } else {
        Debug.LogWarning("[DebugAction] VegetationManager not available");
      }
    }
  }
}

