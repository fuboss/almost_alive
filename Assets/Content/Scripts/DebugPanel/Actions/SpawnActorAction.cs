using System.Linq;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Building.Runtime;
using Content.Scripts.Building.Services;
using Content.Scripts.Descriptors.Tags;
using UnityEngine;

namespace Content.Scripts.DebugPanel.Actions {
  public class SpawnActorAction : IDebugAction {
    private readonly ActorCreationModule _actorCreation;
    private readonly StructuresModule _structuresModule;
    private readonly StructurePlacementService _placement;
    private readonly string _actorKey;

    public SpawnActorAction(ActorCreationModule actorCreation, StructuresModule structuresModule,
      StructurePlacementService structPlacement, string actorKey, string displayName) {
      _actorCreation = actorCreation;
      _actorKey = actorKey;
      _structuresModule = structuresModule;
      _placement = structPlacement;
      this.displayName = displayName;
    }

    public string displayName { get; }
    public DebugCategory category => DebugCategory.Spawn;
    public DebugActionType actionType => DebugActionType.RequiresWorldPosition;

    public string actorKey => _actorKey;

    public void Execute(DebugActionContext context) {
      if (_actorCreation.TrySpawnActorOnGround(_actorKey, context.worldPosition, out var actor)) {
        Debug.Log($"[DebugAction] Spawned {displayName} at {context.worldPosition}");
        
        if (actor.GetComponent<IGoapAgent>() is { } agent) {
          agent.OnCreated();
        }

        if (actor.GetComponent<Structure>() is { } structure) {
          var definition = _structuresModule.definitions.FirstOrDefault(d => d.structureId == actor.actorKey);
          structure.Initialize(definition, 100);
          structure.transform.position = _placement
            .CalculateStructurePosition(structure.transform.position, definition!.footprint, Terrain.activeTerrain, 0.5f);
        }

        return;
      }

      Debug.LogError($"[DebugAction] Failed to spawn {displayName}");
    }
  }
}