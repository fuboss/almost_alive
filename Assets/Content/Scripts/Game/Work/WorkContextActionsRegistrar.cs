using Content.Scripts.Ui.Layers.Inspector;
using UnityEngine;
using VContainer.Unity;

namespace Content.Scripts.Game.Work {
  /// <summary>
  /// Registers default context actions for work system.
  /// </summary>
  public class WorkContextActionsRegistrar : IInitializable {
    public void Initialize() {
      // Woodcutting action for trees
      ContextActionRegistry.RegisterForTag("tree", new ContextAction(
        label: "Mark for Chopping",
        icon: "🪓",
        canExecute: target => {
          var marker = target.gameObject.GetComponent<WorkMarker>();
          return marker == null || !marker.isMarked;
        },
        execute: target => {
          var marker = target.gameObject.GetComponent<WorkMarker>();
          if (marker == null)
            marker = target.gameObject.AddComponent<WorkMarker>();
          marker.Mark(WorkType.WOODCUTTING);
          Debug.Log($"[Work] Marked {target.gameObject.name} for chopping");
        }
      ));

      // Cancel work action
      ContextActionRegistry.RegisterGlobal(new ContextAction(
        label: "Cancel Work",
        icon: "❌",
        canExecute: target => {
          var marker = target.gameObject.GetComponent<WorkMarker>();
          return marker != null && marker.isMarked;
        },
        execute: target => {
          var marker = target.gameObject.GetComponent<WorkMarker>();
          if (marker != null) marker.Unmark();
          Debug.Log($"[Work] Cancelled work on {target.gameObject.name}");
        }
      ));

      // Mining action
      ContextActionRegistry.RegisterForTag("rock", new ContextAction(
        label: "Mark for Mining",
        icon: "⛏️",
        canExecute: target => {
          var marker = target.gameObject.GetComponent<WorkMarker>();
          return marker == null || !marker.isMarked;
        },
        execute: target => {
          var marker = target.gameObject.GetComponent<WorkMarker>();
          if (marker == null)
            marker = target.gameObject.AddComponent<WorkMarker>();
          marker.Mark(WorkType.MINING);
          Debug.Log($"[Work] Marked {target.gameObject.name} for mining");
        }
      ));
    }
  }
}
