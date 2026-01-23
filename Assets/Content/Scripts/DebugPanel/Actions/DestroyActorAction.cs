using Content.Scripts.AI.GOAP;
using Content.Scripts.Building.Services;
using UnityEngine;

namespace Content.Scripts.DebugPanel.Actions {
  public class DestroyActorAction : IDebugAction {
    private readonly ActorDestructionModule _actorDestruction;

    public DestroyActorAction(ActorDestructionModule actorDestruction) {
      _actorDestruction = actorDestruction;
    }

    public string displayName => "Destroy Actor";
    public DebugCategory category => DebugCategory.Destroy;
    public DebugActionType actionType => DebugActionType.RequiresActor;

    public void Execute(DebugActionContext context) {
      if (context.targetActor != null) {
        Debug.Log($"[DebugAction] Destroying {context.targetActor.name}");
        _actorDestruction.DestroyActor(context.targetActor);
      }
    }
  }
  
  public class DestroyStructureAction : IDebugAction {
    private readonly StructuresModule _structuresModule;

    public DestroyStructureAction(StructuresModule structuresModule) {
      _structuresModule = structuresModule;
    }

    public string displayName => "Destroy Structure";
    public DebugCategory category => DebugCategory.Destroy;
    public DebugActionType actionType => DebugActionType.RequiresStructure;

    public void Execute(DebugActionContext context) {
      if (context.targetStructure == null) return;
      
      Debug.Log($"[DebugAction] Destroying {context.targetStructure.name}");
      _structuresModule.DestroyStructure(context.targetStructure);
    }
  }
}

