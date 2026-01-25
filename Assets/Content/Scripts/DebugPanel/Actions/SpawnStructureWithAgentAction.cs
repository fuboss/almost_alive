using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.Services;
using UnityEngine;

namespace Content.Scripts.DebugPanel.Actions {
  /// <summary>
  /// Spawns a structure and an agent next to it at the clicked position.
  /// </summary>
  public class SpawnStructureWithAgentAction : IDebugAction {
    private readonly StructuresModule _structuresModule;
    private readonly ActorCreationModule _actorCreation;
    private readonly StructureDefinitionSO _structureDefinition;
    private readonly string _actorKey;
    private readonly float _agentOffset;

    public SpawnStructureWithAgentAction(
      StructuresModule structuresModule,
      ActorCreationModule actorCreation,
      StructureDefinitionSO structureDefinition,
      string actorKey,
      string displayName,
      float agentOffset = 10f) {
      _structuresModule = structuresModule;
      _actorCreation = actorCreation;
      _structureDefinition = structureDefinition;
      _actorKey = actorKey;
      _agentOffset = agentOffset;
      this.displayName = displayName;
    }

    public string displayName { get; }
    public DebugCategory category => DebugCategory.SpawnTemplates;
    public DebugActionType actionType => DebugActionType.RequiresWorldPosition;

    public void Execute(DebugActionContext context) {
      // Spawn structure at click position
      var structure = _structuresModule.PlaceBlueprint(_structureDefinition, context.worldPosition, 0.5f);
      if (structure == null) {
        Debug.LogError($"[DebugAction] Failed to place structure {_structureDefinition.structureId}");
        return;
      }

      Debug.Log($"[DebugAction] Structure {_structureDefinition.structureId} placed at {context.worldPosition}");

      // Spawn agent next to structure
      var agentPosition = context.worldPosition + Random.onUnitSphere * _agentOffset;
      if (_actorCreation.TrySpawnActorOnGround(_actorKey, agentPosition, out var actor)) {
        Debug.Log($"[DebugAction] Spawned {_actorKey} at {agentPosition}");
        if (actor.GetComponent<IGoapAgent>() is { } agent) {
          agent.OnCreated();
        }
      }
      else {
        Debug.LogError($"[DebugAction] Failed to spawn {_actorKey}");
      }
    }
  }
}

