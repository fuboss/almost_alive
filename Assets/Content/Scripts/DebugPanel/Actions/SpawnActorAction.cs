using Content.Scripts.AI.GOAP;
using UnityEngine;

namespace Content.Scripts.DebugPanel.Actions {
  public class SpawnActorAction : IDebugAction {
    private readonly ActorCreationModule _actorCreation;
    private readonly string _actorKey;

    public SpawnActorAction(ActorCreationModule actorCreation, string actorKey, string displayName) {
      _actorCreation = actorCreation;
      _actorKey = actorKey;
      this.displayName = displayName;
    }

    public string displayName { get; }
    public DebugCategory category => DebugCategory.Spawn;
    public DebugActionType actionType => DebugActionType.RequiresWorldPosition;

    public void Execute(DebugActionContext context) {
      if (_actorCreation.TrySpawnActorOnGround(_actorKey, context.worldPosition, out var actor)) {
        Debug.Log($"[DebugAction] Spawned {displayName} at {context.worldPosition}");
      } else {
        Debug.LogError($"[DebugAction] Failed to spawn {displayName}");
      }
    }
  }
}