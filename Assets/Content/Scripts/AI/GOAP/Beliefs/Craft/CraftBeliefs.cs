using System;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Building.Runtime;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Craft {
  //todo: remove CAMP usages, use structure-based queries instead

  [Serializable, TypeInfoBox("True when has active unfinished actor.")]
  public class HasActiveUnfinishedBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      return () => {
        var result = UnfinishedQuery.HasActiveUnfinished();
        return inverse ? !result : result;
      };
    }

    public override AgentBelief Copy() => new HasActiveUnfinishedBelief { name = name, inverse = inverse };
  }

  [Serializable, TypeInfoBox("True when unfinished needs resources.")]
  public class UnfinishedNeedsResourcesBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      return () => {
        var result = UnfinishedQuery.GetNeedingResources() != null;
        return !inverse ? result : !result;
      };
    }

    public override AgentBelief Copy() => new UnfinishedNeedsResourcesBelief { name = name, inverse = inverse };
  }

  [Serializable, TypeInfoBox("True when unfinished has all resources and needs work.")]
  public class UnfinishedNeedsWorkBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      return () => {
        var unfinished = UnfinishedQuery.GetNeedingWork();
        var result = unfinished != null;
        return !inverse ? result : !result;
      };
    }

    public override AgentBelief Copy() => new UnfinishedNeedsWorkBelief { name = name, inverse = inverse };
  }

  [Serializable, TypeInfoBox("True when unfinished is ready to complete (has resources + work done).")]
  public class UnfinishedReadyToCompleteBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      return () => {
        var result = UnfinishedQuery.GetReadyToComplete() != null;
        return inverse ? !result : result;
      };
    }

    public override AgentBelief Copy() => new UnfinishedReadyToCompleteBelief { name = name, inverse = inverse };
  }

  [Serializable, TypeInfoBox("True when agent's inventory has at least one resource needed by unfinished.")]
  public class InventoryHasResourcesForUnfinishedBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      if (agent is not IInventoryAgent invAgent) return () => false;

      return () => {
        var target = UnfinishedQuery.GetNeedingResources();
        if (target == null) return false;

        var needs = target.GetRemainingResources()
          .Where(n => invAgent.inventory.GetItemCount(n.tag) > 0);
        var result = needs.Any();
        return inverse ? !result : result;
      };
    }

    public override AgentBelief Copy() => new InventoryHasResourcesForUnfinishedBelief { name = name, inverse = inverse };
  }

  [Serializable, TypeInfoBox("True when camp storage has at least one resource needed by unfinished.")]
  public class StorageHasResourcesForUnfinishedBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      return () => {
        var result = AnyStorageHasRequiredResources();
        return inverse ? !result : result;
      };
    }

    private static bool AnyStorageHasRequiredResources() {
      var unfinished = UnfinishedQuery.GetNeedingResources();
      if (unfinished == null) return false;
      var needs = unfinished.GetRemainingResources();

      var allStorages = ActorRegistry<StorageActor>.all;
      foreach (var (tag, _) in needs) {
        var hasInStorage = allStorages.Any(s => s.GetCountWithTag(tag) > 0);
        if (hasInStorage) return true;
      }

      return false;
    }

    public override AgentBelief Copy() => new StorageHasResourcesForUnfinishedBelief { name = name, inverse = inverse };
  }

  [Serializable,
   TypeInfoBox("True when unfinished needs resources that must be gathered (not enough in inventory + storage).")]
  public class NeedsGatherForUnfinishedBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      if (agent is not IInventoryAgent invAgent) return () => false;

      return () => {
        var result = IsNotEnoughResources(invAgent);
        return inverse ? !result : result;
      };
    }

    private static bool IsNotEnoughResources(IInventoryAgent invAgent) {
      var target = UnfinishedQuery.GetNeedingResources();
      if (target == null) return false;

      var needs = target.GetRemainingResources();
      var allStorages = ActorRegistry<StorageActor>.all;
      foreach (var (tag, needed) in needs) {
        var inInventory = invAgent.inventory.GetItemCount(tag);
        var inStorage = allStorages.Sum(s => s.GetCountWithTag(tag));

        if (inInventory + inStorage < needed) return true;
      }

      return false;
    }

    private int GetItemCountInStorage(string tag) {
      return ActorRegistry<StorageActor>.all
        .Sum(s => s.GetCountWithTag(tag));
    }

    public override AgentBelief Copy() => new NeedsGatherForUnfinishedBelief { name = name, inverse = inverse };
  }

  [Serializable, TypeInfoBox("True when camp needs new unfinished (has empty spots and agent has unlocked recipes).")]
  public class CanStartCraftingOnStructuresEmptySlotsBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      if (agent is not IWorkAgent workAgent) return () => false;

      return () => {
        var result = Check(workAgent);
        return inverse ? !result : result;
      };
    }

    private static bool Check(IWorkAgent workAgent) {
      if (UnfinishedQuery.HasActiveUnfinished()) return false;
      var campRecipes = workAgent.recipes.GetUnlockedRecipes(workAgent.recipeModule);
      //todo: optimize by caching
      var allEmptySlots = Registry<Building.Runtime.Structure>
        .GetAll()
        .SelectMany(st => st.GetEmptySlots())
        .Where(slot => !slot.isLocked)
        .ToArray();

      foreach (var recipe in campRecipes) {
        var resultActorTags = workAgent.recipeModule.GetResultActorTags(recipe);

        if (resultActorTags.Length > 0) {
          foreach (var emptySlot in allEmptySlots) {
            var accepts = emptySlot.IsAcceptingTags(resultActorTags);
            if (accepts) return true;
          }
        }
        else {
          if (allEmptySlots.Length > 0) return true;
        }
      }

      return false;
    }

    public override AgentBelief Copy() => new CanStartCraftingOnStructuresEmptySlotsBelief
      { name = name, inverse = inverse };
  }

  [Serializable, TypeInfoBox("True when agent has any unlocked hand-craftable recipe (ignores resources).")]
  public class HasHandCraftableRecipeBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      if (agent is not IWorkAgent workAgent) return () => false;

      return () => {
        var handRecipes = workAgent.recipeModule.GetHandCraftable();
        var result = handRecipes.Any(r => workAgent.recipes.IsUnlocked(r));
        return inverse ? !result : result;
      };
    }

    public override AgentBelief Copy() => new HasHandCraftableRecipeBelief { name = name, inverse = inverse };
  }

  [Serializable, TypeInfoBox("True when can remember any craft resource needed by unfinished.")]
  public class MemoryHasCraftResourceBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      return () => {
        var allUnfinished = UnfinishedQuery.GetAllNeedingResources().ToArray();
        if (allUnfinished.Length == 0) return inverse;

        var found = false;
        foreach (var unfinishedActor in allUnfinished) {
          var remainingResourcesTags = unfinishedActor
            .GetRemainingResources()
            .Select(r => r.tag)
            .ToArray();
          if (agent.memory.GetWithAnyTags(remainingResourcesTags).Length <= 0) continue;
          found = true;
          break;
        }

        var result = inverse ? !found : found;

        return result;
      };
    }

    public override AgentBelief Copy() => new MemoryHasCraftResourceBelief { name = name, inverse = inverse };
  }
}