using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.Game.Storage;

namespace Content.Scripts.Game.Craft {
  public static class UnfinishedQuery {

    public static bool HasActiveUnfinished() {
      return ActorRegistry<UnfinishedActorBase>.count > 0;
    }

    public static IUnfinishedActor GetNeedingResource(string tag) {
      return ActorRegistry<UnfinishedActorBase>.all
        .FirstOrDefault(u => u.GetRemainingResourceCount(tag) > 0);
    }

    public static IUnfinishedActor GetNeedingResources() {
      return ActorRegistry<UnfinishedActorBase>.all
        .FirstOrDefault(u => !u.hasAllResources);
    }

    public static IEnumerable<IUnfinishedActor> GetAllNeedingResources() {
      return ActorRegistry<UnfinishedActorBase>.all
        .Where(u => !u.hasAllResources);
    }

    public static IUnfinishedActor GetNeedingWork() {
      return ActorRegistry<UnfinishedActorBase>.all
        .FirstOrDefault(u => u.hasAllResources && !u.workComplete);
    }

    public static IUnfinishedActor GetReadyToComplete() {
      return ActorRegistry<UnfinishedActorBase>.all
        .FirstOrDefault(u => u.isReadyToComplete);
    }

    public static HashSet<string> GetAllRequiredResourceTags() {
      var tags = new HashSet<string>();
      foreach (var unfinished in GetAllNeedingResources()) {
        var remaining = unfinished.GetRemainingResources();
        if (remaining == null) continue;
        foreach (var (tag, _) in remaining) {
          tags.Add(tag);
        }
      }
      return tags;
    }

    public static bool IsResourceNeeded(string tag) {
      return GetNeedingResource(tag) != null;
    }

    public static bool InventoryHasAnyNeeded(ActorInventory inventory) {
      var target = GetNeedingResources();
      if (target == null) return false;

      var needs = target.GetRemainingResources();
      if (needs == null) return false;

      foreach (var (tag, _) in needs) {
        if (inventory.GetItemCount(tag) > 0) return true;
      }
      return false;
    }

    public static bool StorageHasAnyNeeded() {
      var target = GetNeedingResources();
      if (target == null) return false;

      var needs = target.GetRemainingResources();
      if (needs == null) return false;

      var allStorages = ActorRegistry<StorageActor>.all;
      foreach (var (tag, _) in needs) {
        foreach (var storage in allStorages) {
          if (storage.GetCountWithTag(tag) > 0) return true;
        }
      }
      return false;
    }

    public static bool NeedsGathering(ActorInventory inventory) {
      var target = GetNeedingResources();
      if (target == null) return false;

      var needs = target.GetRemainingResources();
      if (needs == null) return false;

      var allStorages = ActorRegistry<StorageActor>.all;
      foreach (var (tag, needed) in needs) {
        var inInventory = inventory.GetItemCount(tag);
        var inStorage = allStorages.Sum(s => s.GetCountWithTag(tag));
        if (inInventory + inStorage < needed) return true;
      }
      return false;
    }

    public static bool MemoryHasAnyNeeded(AgentMemory memory) {
      foreach (var unfinished in GetAllNeedingResources()) {
        var remaining = unfinished.GetRemainingResources();
        if (remaining == null) continue;

        var tags = remaining.Select(r => r.tag).ToArray();
        if (memory.GetWithAnyTags(tags).Length > 0) return true;
      }
      return false;
    }
  }
}