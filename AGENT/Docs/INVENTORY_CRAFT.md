# Inventory & Craft

## Inventory

**ActorInventory** - stacking support, FilteredActorInventory for storages

```csharp
inventory.isFull, isEmpty, freeSlotCount
slot.TryStack(item), slot.RemoveCount(5)
```

---

## Storage & Hauling

**StorageActor** - FilteredActorInventory + ActorPriority

**StorageQuery** - helpers with frame-based cache

Beliefs: Storage_NeedsFilling, Inventory_HasHaulable, InventoryReadyForDeposit

Strategies: BatchCollectStrategy, MoveToBestStorageStrategy, DepositToStorageStrategy

---

## Craft

**UnfinishedActor** - intermediate state (recipe, resources, work progress)

**UnfinishedQuery** - helpers

Strategies: PlaceUnfinished, DeliverToUnfinished, WorkOnUnfinished

Beliefs: HasActiveUnfinished, UnfinishedNeedsResources, UnfinishedNeedsWork, CanDeliverToUnfinished

---

## Recipes

**RecipeSO:** resultActorKey, requiredResources, craftTime, stationType, isCampRecipe, campBuildPriority

**RecipeModule API:**
```csharp
GetByResult(actorKey)
GetByStation(stationType)
GetHandCraftable()
CanCraft(recipe, inventory)
```

---

## Progression

**AgentExperience** - level, XP  
**AgentRecipes** - unlocked recipes per agent  
**RecipeProgressionSO** - config at Resources/Recipes/RecipeProgression
