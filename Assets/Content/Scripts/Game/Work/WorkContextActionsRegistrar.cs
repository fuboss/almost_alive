using Content.Scripts.AI;
using Content.Scripts.Game.Harvesting;
using Content.Scripts.Game.Interaction;
using Content.Scripts.Ui.Layers.Inspector;
using UnityEngine;
using VContainer.Unity;

namespace Content.Scripts.Game.Work {
  /// <summary>
  /// Registers default context actions for work system.
  /// </summary>
  public class WorkContextActionsRegistrar : IInitializable {
    public void Initialize() {
      RegisterWoodcuttingActions();
      RegisterMiningActions();
      RegisterFarmingActions();
      RegisterCancelAction();
    }

    private void RegisterWoodcuttingActions() {
      // Chop Tree — mark for woodcutting
      ContextActionRegistry.RegisterForTag(Tag.TREE, new ContextAction(
        label: "Chop Tree",
        icon: "🪓",
        canExecute: target => !IsMarkedForWork(target),
        execute: target => MarkForWork(target, WorkType.WOODCUTTING, "chopping")
      ));

      // Chop All Trees — mark all trees for woodcutting
      ContextActionRegistry.RegisterForTag(Tag.TREE, new ContextAction(
        label: "Chop All Trees",
        icon: "🌲",
        canExecute: _ => HasAnyUnmarkedTree(),
        execute: _ => MarkAllTreesForWork()
      ));
    }

    private void RegisterMiningActions() {
      // Mine Rock — mark for mining
      ContextActionRegistry.RegisterForTag(Tag.STONE, new ContextAction(
        label: "Mine Rock",
        icon: "⛏️",
        canExecute: target => !IsMarkedForWork(target),
        execute: target => MarkForWork(target, WorkType.MINING, "mining")
      ));
    }

    private void RegisterFarmingActions() {
      // Harvest — mark harvestable for gathering
      ContextActionRegistry.RegisterForTag(Tag.HARVESTABLE, new ContextAction(
        label: "Harvest",
        icon: "🌿",
        canExecute: target => !IsMarkedForWork(target) && HarvestModule.HasYield(target as ActorDescription),
        execute: target => MarkForWork(target, WorkType.FARMING, "harvesting")
      ));

      // Harvest All — mark all harvestables with yield
      ContextActionRegistry.RegisterForTag(Tag.HARVESTABLE, new ContextAction(
        label: "Harvest All Ready",
        icon: "🧺",
        canExecute: _ => HasAnyUnmarkedHarvestable(),
        execute: _ => MarkAllHarvestablesForWork()
      ));
    }

    private void RegisterCancelAction() {
      // Cancel work — global action
      ContextActionRegistry.RegisterGlobal(new ContextAction(
        label: "Cancel Work",
        icon: "❌",
        canExecute: IsMarkedForWork,
        execute: target => {
          var marker = target.gameObject.GetComponent<WorkMarker>();
          marker?.Unmark();
          Debug.Log($"[Work] Cancelled work on {target.gameObject.name}");
        }
      ));
    }

    #region Helpers

    private static bool IsMarkedForWork(ISelectableActor target) {
      var marker = target.gameObject.GetComponent<WorkMarker>();
      return marker != null && marker.isMarked;
    }

    private static void MarkForWork(ISelectableActor target, WorkType workType, string actionName) {
      var marker = target.gameObject.GetComponent<WorkMarker>();
      if (marker == null)
        marker = target.gameObject.AddComponent<WorkMarker>();
      marker.Mark(workType);
      Debug.Log($"[Work] Marked {target.gameObject.name} for {actionName}");
    }

    private static bool HasAnyUnmarkedHarvestable() {
      foreach (var growth in ActorRegistry<GrowthProgress>.all) {
        if (growth == null || !growth.hasYield) continue;
        var marker = growth.GetComponent<WorkMarker>();
        if (marker == null || !marker.isMarked) return true;
      }
      return false;
    }

    private static void MarkAllHarvestablesForWork() {
      var count = 0;
      foreach (var growth in ActorRegistry<GrowthProgress>.all) {
        if (growth == null || !growth.hasYield) continue;
        
        var marker = growth.GetComponent<WorkMarker>();
        if (marker != null && marker.isMarked) continue;
        
        var actor = growth.GetComponent<ActorDescription>();
        if (actor == null) continue;
        
        MarkForWork(actor, WorkType.FARMING, "harvesting");
        count++;
      }
      Debug.Log($"[Work] Marked {count} harvestables for harvesting");
    }

    private static bool HasAnyUnmarkedTree() {
      foreach (var tree in ActorRegistry<TreeTag>.all) {
        if (tree == null) continue;
        var marker = tree.GetComponent<WorkMarker>();
        if (marker == null || !marker.isMarked) return true;
      }
      return false;
    }

    private static void MarkAllTreesForWork() {
      var count = 0;
      foreach (var tree in ActorRegistry<TreeTag>.all) {
        if (tree == null) continue;
        
        var marker = tree.GetComponent<WorkMarker>();
        if (marker != null && marker.isMarked) continue;
        
        var actor = tree.GetComponent<ActorDescription>();
        if (actor == null) continue;
        
        MarkForWork(actor, WorkType.WOODCUTTING, "chopping");
        count++;
      }
      Debug.Log($"[Work] Marked {count} trees for chopping");
    }

    #endregion
  }
}
