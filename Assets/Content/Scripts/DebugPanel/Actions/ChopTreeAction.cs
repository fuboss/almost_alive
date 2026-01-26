using Content.Scripts.Game;
using Content.Scripts.Game.Trees;
using UnityEngine;

namespace Content.Scripts.DebugPanel.Actions {
  public class ChopTreeAction : IDebugAction {
    private readonly TreeModule _treeModule;

    public ChopTreeAction(TreeModule treeModule) {
      _treeModule = treeModule;
    }

    public string displayName => "Chop Tree (Fall)";
    public DebugCategory category => DebugCategory.Environment;
    public DebugActionType actionType => DebugActionType.RequiresActor;

    public void Execute(DebugActionContext context) {
      if (context.targetActor == null) return;

      var treeTag = context.targetActor.GetDefinition<TreeTag>();
      if (treeTag == null) {
        Debug.LogWarning("[ChopTreeAction] Target is not a tree");
        return;
      }

      Debug.Log($"[ChopTreeAction] Chopping tree: {context.targetActor.name}");
      _treeModule.StartTreeFall(context.targetActor, treeTag, context.worldPosition);
    }
  }
}
