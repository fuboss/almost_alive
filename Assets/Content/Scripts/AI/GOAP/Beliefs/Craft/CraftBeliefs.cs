using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Content.Scripts.Game.Storage;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Content.Scripts.AI.GOAP.Beliefs.Craft {
  [Serializable, TypeInfoBox("True when camp has active unfinished actor.")]
  public class HasActiveUnfinishedBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return UnfinishedQuery.HasActiveUnfinished(camp);
      };
    }

    public override AgentBelief Copy() => new HasActiveUnfinishedBelief { name = name };
  }

  [Serializable, TypeInfoBox("True when unfinished needs resources.")]
  public class UnfinishedNeedsResourcesBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return UnfinishedQuery.GetNeedingResources(camp) != null;
      };
    }

    public override AgentBelief Copy() => new UnfinishedNeedsResourcesBelief { name = name };
  }

  [Serializable, TypeInfoBox("True when unfinished has all resources and needs work.")]
  public class UnfinishedNeedsWorkBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return UnfinishedQuery.GetNeedingWork(camp) != null;
      };
    }

    public override AgentBelief Copy() => new UnfinishedNeedsWorkBelief { name = name };
  }

  [Serializable, TypeInfoBox("True when unfinished is ready to complete (has resources + work done).")]
  public class UnfinishedReadyToCompleteBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        return UnfinishedQuery.GetReadyToComplete(camp) != null;
      };
    }

    public override AgentBelief Copy() => new UnfinishedReadyToCompleteBelief { name = name };
  }

  [Serializable, TypeInfoBox("True when agent can deliver craft resources (has them in storages).")]
  public class CanDeliverFromStorageToUnfinishedBelief : CanDeliverToUnfinishedBelief {
    protected override bool Search(IGoapAgent agent, UnfinishedActor target) {
      var needs = target.GetRemainingResources();
      var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
      if (!deliverAllNeededTypes) {
        bool foundAny = false;
        foreach (var (tag, _) in needs) {
          var countInStorage = GetResourceCountInCampStorages(camp, tag);
          if (countInStorage >= countThreshold) {
            foundAny = true;
            break;
          }
        }

        return foundAny;
      }

      Dictionary<string, int> found = new();

      foreach (var tag in needs.Select(n => n.tag)) {
        found[tag] = GetResourceCountInCampStorages(camp, tag);
      }

      // Check if all needs can be fulfilled from inventory
      bool allFulfilled = true;
      foreach (var (tag, remaining) in needs) {
        if (found[tag] < remaining) {
          allFulfilled = false;
          break;
        }
      }

      return allFulfilled;
    }

    //todo: optimize by caching storages per camp
    private int GetResourceCountInCampStorages(CampLocation camp, string tag) {
      if (camp == null) return 0;
      var campPos = camp.transform.position;

      return ActorRegistry<StorageActor>.all
        .Where(s => Vector3.Distance(s.transform.position, campPos) < 30f)
        .Sum(s => s.GetCountWithTag(tag));
    }
    
    public override AgentBelief Copy() => new CanDeliverFromStorageToUnfinishedBelief {
      name = name,
      countThreshold = countThreshold
    };
  }

  [Serializable, TypeInfoBox("True when agent can deliver craft resources\n (has them in inventory).")]
  public class CanDeliverToUnfinishedBelief : AgentBelief {
    public int countThreshold = 1;
    public bool deliverAllNeededTypes = false;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        var target = UnfinishedQuery.GetNeedingResources(camp);
        return target != null && Search(agent, target);
      };
    }

    protected virtual bool Search(IGoapAgent agent, UnfinishedActor target) {
      var needs = target.GetRemainingResources();
      if (!deliverAllNeededTypes) {
        foreach (var (tag, _) in needs) {
          if (agent.inventory.GetItemCount(tag) >= countThreshold) {
            return true;
          }
          //if (checkCampStorage && HasResourceInCampStorages(camp, tag)) return true;
        }
      }
      else {
        var left = needs.ToDictionary(n => n.tag, n => n.remaining);
        Dictionary<string, int> found = new();

        foreach (var leftKey in left.Keys) {
          var countInInventory = agent.inventory.GetItemCount(leftKey);
          found[leftKey] = countInInventory;
        }

        // Check if all needs can be fulfilled from inventory
        bool allFulfilled = true;
        foreach (var (tag, remaining) in left) {
          if (found[tag] < remaining) {
            allFulfilled = false;
            break;
          }
        }

        return allFulfilled;
      }

      return false;
    }

    public override AgentBelief Copy() => new CanDeliverToUnfinishedBelief {
      name = name,
      countThreshold = countThreshold
    };
  }

  [Serializable, TypeInfoBox("True when agent's inventory has at least one resource needed by unfinished.")]
  public class InventoryHasForUnfinishedBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        var target = UnfinishedQuery.GetNeedingResources(camp);
        if (target == null) return false;

        var needs = target.GetRemainingResources();
        return needs.Any(n => agent.inventory.GetItemCount(n.tag) > 0);
      };
    }

    public override AgentBelief Copy() => new InventoryHasForUnfinishedBelief { name = name };
  }

  [Serializable, TypeInfoBox("True when camp storage has at least one resource needed by unfinished.")]
  public class StorageHasForUnfinishedBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        var target = UnfinishedQuery.GetNeedingResources(camp);
        if (target == null) return false;

        var campPos = camp.transform.position;
        var needs = target.GetRemainingResources();

        foreach (var (tag, _) in needs) {
          var hasInStorage = ActorRegistry<StorageActor>.all
            .Any(s => Vector3.Distance(s.transform.position, campPos) < 30f &&
                      s.GetCountWithTag(tag) > 0);
          if (hasInStorage) return true;
        }

        return false;
      };
    }

    public override AgentBelief Copy() => new StorageHasForUnfinishedBelief { name = name };
  }

  [Serializable,
   TypeInfoBox("True when unfinished needs resources that must be gathered (not enough in inventory + storage).")]
  public class NeedsGatherForUnfinishedBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        var target = UnfinishedQuery.GetNeedingResources(camp);
        if (target == null) return false;

        var campPos = camp.transform.position;
        var needs = target.GetRemainingResources();

        foreach (var (tag, needed) in needs) {
          var inInventory = agent.inventory.GetItemCount(tag);
          var inStorage = GetStorageCount(campPos, tag);

          if (inInventory + inStorage < needed) return true;
        }

        return false;
      };
    }

    private int GetStorageCount(Vector3 campPos, string tag) {
      return ActorRegistry<StorageActor>.all
        .Where(s => Vector3.Distance(s.transform.position, campPos) < 30f)
        .Sum(s => s.GetCountWithTag(tag));
    }

    public override AgentBelief Copy() => new NeedsGatherForUnfinishedBelief { name = name };
  }

  [Serializable, TypeInfoBox("True when camp needs new unfinished (has empty spots and agent has unlocked recipes).")]
  public class CampNeedsNewUnfinishedBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        if (camp?.setup == null) return false;

        if (UnfinishedQuery.HasActiveUnfinished(camp)) return false;
        if (camp.setup.allSpotsFilled) return false;

        var campRecipes = agent.recipes.GetUnlockedCampRecipes(agent.recipeModule);

        foreach (var recipe in campRecipes) {
          var tags = agent.recipeModule.GetResultActorTags(recipe);
          var tag = tags?.Length > 0 ? tags[0] : null;

          if (!string.IsNullOrEmpty(tag)) {
            if (camp.setup.GetSpotsNeedingTag(tag).Any()) return true;
          }
          else {
            if (camp.setup.GetAnyEmptySpot() != null) return true;
          }
        }

        return false;
      };
    }

    public override AgentBelief Copy() => new CampNeedsNewUnfinishedBelief {
      name = name,
    };
  }

  [Serializable, TypeInfoBox("True when agent has any unlocked hand-craftable recipe (ignores resources).")]
  public class HasHandCraftableRecipeBelief : AgentBelief {
    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var handRecipes = agent.recipeModule.GetHandCraftable();
        return handRecipes.Any(r => agent.recipes.IsUnlocked(r));
      };
    }

    public override AgentBelief Copy() => new HasHandCraftableRecipeBelief {
      name = name,
    };
  }

  [Serializable, TypeInfoBox("True when can rember any craft resource needed by unfinished.")]
  public class MemoryHasCraftResourceBelief : AgentBelief {
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgent agent) {
      return () => {
        var camp = agent.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
        var target = UnfinishedQuery.GetNeedingResources(camp);
        if (target == null) return false;

        var needs = target.GetRemainingResources();
        var check = needs.Select(n => n.tag)
          .Select(tag => agent.memory.GetWithAnyTags(new[] { tag })).Any();
        return inverse ? !check : check;
      };
    }

    public override AgentBelief Copy() => new MemoryHasCraftResourceBelief { name = name };
  }
}