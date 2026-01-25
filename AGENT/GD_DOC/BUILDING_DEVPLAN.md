# Building System — Development Plan

> Extracted from BUILDING.md for tracking implementation progress.

## Status: 🟡 Phase 4.5 — Multi-Slot Modules

---

## Phase 1: Data Foundation ✅ DONE
**Goal:** Core data structures, no runtime yet.

- [x] 1.1 `StructureDefinitionSO` — scriptable object with footprint, foundationPrefab, slots[], constructionData
- [x] 1.2 `SlotDefinition` — serializable class with slotId, slotType, localPosition/rotation, acceptedModuleTags[], isInterior, startsLocked
- [x] 1.3 `ModuleDefinitionSO` — scriptable object with moduleId, tags[], compatibleSlotTypes[], recipe, prefab, deconstructReturnPercent
- [x] 1.4 `SlotType` enum — Sleeping, Production, Storage, Utility, Entertainment
- [ ] 1.5 Test assets — StructureDefinition_BasicShelter.asset, ModuleDefinition_Bedroll.asset, _Chest.asset

---

## Phase 2: Runtime Structure + Editor Tooling ✅ DONE

### 2.A Editor Tooling ✅ DONE

- [x] 2.A.1 `StructureFoundationBuilder.cs` — editor assembly helper
- [x] 2.A.2 `StructureFoundationBuilderEditor.cs` — custom editor with buttons
- [x] 2.A.3 Terrain Check Visualization
- [x] 2.A.4 Slot visualization colors
- [x] 2.A.5 `StructureTag.cs` — prefab metadata component (renamed from StructureDescription)
- [x] 2.A.6 `StructurePartDescription.cs` — wall prefab metadata
- [x] 2.A.7 Save as Addressable Prefab workflow

### 2.B Runtime Data Classes ✅ DONE

- [x] 2.B.1 `WallSide` enum
- [x] 2.B.2 `WallSegmentType` enum
- [x] 2.B.3 `WallSegment` class
- [x] 2.B.4 `EntryPoint` class
- [x] 2.B.5 `BuildingConstants` static class
- [x] 2.B.7 `Slot` class
- [x] 2.B.8 `Module` MonoBehaviour

### 2.C Architecture Refactor ✅ DONE
**Goal:** SOLID decomposition — separate data from logic.

#### Data Layer
- [x] 2.C.1 `IConstructionRequirements` interface
- [x] 2.C.2 `ConstructionData` class
- [x] 2.C.3 Update `RecipeData` — implement IConstructionRequirements
- [x] 2.C.4 Update `StructureDefinitionSO` — use ConstructionData

#### Runtime Layer (refactored)
- [x] 2.C.5 `Structure` (MonoBehaviour) — data-only
- [x] 2.C.6 `UnfinishedStructureActor` (MonoBehaviour) — blueprint + progress (extends UnfinishedActorBase)

#### Services Layer (DI)
- [x] 2.C.7 `StructurePlacementService` — terrain, ghost
- [x] 2.C.8 `StructureConstructionService` — building logic
- [x] 2.C.9 `StructuresModule` — main coordinator
- [x] 2.C.10 Register services in `GameScope`

- [x] 2.C.11 Test: full construction flow via services

---

## Phase 3: Placement System (DEFERRED)
**Goal:** Player can place structure blueprints via UI.
**Note:** Using DebugPanel for now.

- [ ] 3.1 `StructurePlacementController` — placement mode, ghost preview, grid snap
- [ ] 3.2 Placement Validation — terrain, obstacles, navmesh checks
- [ ] 3.3 Placement Confirmation — calls StructuresModule.PlaceBlueprint()
- [ ] 3.4 Input integration — ESC cancel, R rotate

---

## Phase 4: Construction Flow (GOAP) ✅ DONE
**Goal:** Agents build structures autonomously.

### 4.0 Unify Craft System ✅ DONE
- [x] `UnfinishedActorBase` — abstract base for all unfinished actors
- [x] `UnfinishedActor` — generic craft items
- [x] `UnfinishedStructureActor` — structures (extends base)
- [x] `UnfinishedQuery` — unified queries for all unfinished types
- [x] `IUnfinishedActor` interface

### 4.1 Beliefs ✅ DONE (in CraftBeliefs.cs — unified for all craft)
- [x] `HasActiveUnfinishedBelief`
- [x] `UnfinishedNeedsResourcesBelief`
- [x] `UnfinishedNeedsWorkBelief`
- [x] `UnfinishedReadyToCompleteBelief`
- [x] `InventoryHasResourcesForUnfinishedBelief`
- [x] `StorageHasResourcesForUnfinishedBelief`
- [x] `NeedsGatherForUnfinishedBelief`
- [x] `MemoryHasCraftResourceBelief`
- [x] `CanStartCraftingOnStructuresEmptySlotsBelief`

### 4.2 Strategies ✅ DONE (in Strategies/Craft/)
- [x] `DeliverToUnfinishedStrategy` — delivers resources to nearest UnfinishedActorBase
- [x] `WorkOnUnfinishedStrategy` — works on unfinished, completes when ready
- [x] `MoveToBestUnfinishedStrategy` — moves to nearest unfinished

### 4.3 Agent Interface ✅ DONE
- [x] `IBuilderAgent` interface
- [x] Add to `GOAPAgent`

### 4.4 Debug Actions ✅ DONE
- [x] `SpawnStructureAction` — spawn structure via DebugPanel
- [x] `SpawnStructureWithAgentAction` — spawn structure + agent

### 4.5 Action/Goal Assets (TODO — low priority)
- [ ] Create `action_DeliverToUnfinished.asset`
- [ ] Create `action_WorkOnUnfinished.asset`
- [ ] Create `goal_BuildStructure.asset`
- [ ] Create `Structure_FeatureSet.asset`

---

## Phase 4.5: Multi-Slot Modules ✅ DONE
**Goal:** Module actors can occupy multiple slots (footprint).

### 4.5.1 Data Layer Changes ✅
- [x] `ModuleDefinitionSO` — added `Vector2Int slotFootprint` (default 1x1)
- [x] `ModuleDefinitionSO` — added `int clearanceRadius` for pathfinding zone
- [x] `ModuleDefinitionSO` — removed `prefab`, using `recipe.resultActorKey` instead
- [x] `StructureDefinitionSO` — added `coreModule` (required module before others)
- [x] `StructureDefinitionSO` — added `coreModuleSlotIds[]`

### 4.5.2 Runtime Slot Logic ✅
- [x] `Slot` — new state `OCCUPIED` for non-anchor slots
- [x] `Slot` — added `anchorSlot` reference
- [x] `Slot.AssignAsAnchor()`, `AssignAsOccupied()`, `ClearOccupied()`
- [x] `Slot.isAnchor`, `isInUse`, `GetModule()`, `GetAssignedModuleDef()`

### 4.5.3 Structure Query Methods ✅
- [x] `Structure.FindSlotsForModule()` — finds contiguous slots that fit footprint
- [x] `Structure.CanPlaceModule()` — validates placement + clearance + core
- [x] `Structure.AssignModuleToSlots()` — assigns anchor + occupied slots
- [x] `Structure.GetSlotsForModule()`, `ClearModule()`
- [x] `Structure.isCoreBuilt`, `requiresCore`, `SetCoreBuilt()`

### 4.5.4 Clearance Validation ✅
- [x] `Structure.ValidateClearance()` — checks ring around module
- [x] Slots outside structure bounds (walls) are ignored
- [x] Grid-based position calculation

### 4.5.5 Module Placement Service ✅
- [x] `ModulePlacementService` — handles module placement logic
- [x] `PlaceModule()`, `PlaceModuleAt()`, `PlaceCoreModule()`
- [x] `RemoveModule()` — clears all slots, resets core status if needed
- [x] Uses `ActorCreationModule` to spawn (not Instantiate)
- [x] Registered in `GameScope`

### 4.5.6 Debug Panel Integration ✅
- [x] `PlaceModuleAction` — place module via DebugPanel
- [x] `DebugCategory.Module` added
- [x] `DebugActionType.RequiresStructure` handling
- [x] `DebugModule.TryGetStructureUnderMouse()`
- [x] `DebugModule.RegisterModuleActions()` + event subscription
- [x] `StructuresModule` loads `ModuleDefinitionSO` from Addressables

### 4.5.7 Test Scenarios (Manual)
- [ ] Create test ModuleDefinition assets with Addressable label
- [ ] Place 1x1 module in single slot
- [ ] Place 2x1 module occupying 2 adjacent slots
- [ ] Validate clearance prevents placement next to occupied slots
- [ ] Validate core module required before other modules
- [ ] Remove module frees all occupied slots

---

## Phase 5: Module Construction Flow ✅ DONE
**Goal:** Modules go through UnfinishedActor flow like structures.

### 5.1 UnfinishedModuleActor ✅
- [x] `UnfinishedModuleActor` — extends UnfinishedActorBase
- [x] Stores: `targetStructure`, `anchorSlot`, `ModuleDefinitionSO`
- [x] `TryComplete()` → calls `ModulePlacementService.CompleteModule()`
- [x] `OnDestroy()` — clears slot assignment if destroyed without completion
- [x] Inherits `ActorRegistry<UnfinishedActorBase>` registration

### 5.2 ModulePlacementService Updates ✅
- [x] `AssignModule()` — assigns slots + creates UnfinishedModuleActor
- [x] `AssignModuleAt()` — at specific anchor slot
- [x] `AssignCoreModule()` — for core module
- [x] `CompleteModule()` — called by UnfinishedModuleActor.TryComplete()
- [x] `InstantPlaceModule()` — for debug/cheats (no construction)
- [x] `InstantPlaceModuleAt()`, `InstantPlaceCoreModule()` — instant variants
- [x] Injects dependencies into UnfinishedModuleActor

### 5.3 DebugPanel Integration ✅
- [x] `PlaceModuleAction` — instant placement (debug)
- [x] `AssignModuleAction` — assignment for construction
- [x] Both registered for each ModuleDefinition

### 5.4 Integration with Craft Flow ✅
- [x] `UnfinishedQuery` works via `ActorRegistry<UnfinishedActorBase>` — auto picks up UnfinishedModuleActor
- [x] Existing beliefs (`UnfinishedNeedsResourcesBelief`, etc.) work automatically
- [x] Existing strategies (`DeliverToUnfinishedStrategy`, `WorkOnUnfinishedStrategy`) work automatically

### 5.5 Required Prefab
- [ ] Create `unfinished_module` prefab with:
  - `ActorDescription` (actorKey: "unfinished_module")
  - `ActorInventory`
  - `UnfinishedModuleActor`
  - Visual placeholder (optional)
- [ ] Add to Addressables

---

## Phase 6: Player Module Assignment (DEFERRED)
**Goal:** Player can assign modules to slots via UI.

- [ ] 6.1 Structure Selection — click to select, info panel
- [ ] 6.2 Slot Visualization — highlight slots, color coding
- [ ] 6.3 Assignment UI — popup with compatible modules
- [ ] 6.4 Priority Control — Low/Normal/High/Critical

---

## Phase 7: Agent Autonomous Building (DEFERRED)
**Goal:** Agents decide what to build without player input.

- [ ] 7.1 Need Evaluation — map needs to module tags
- [ ] 7.2 Slot Selection — find compatible empty slots
- [ ] 7.3 Auto-Assignment — agent assigns, lower priority than player
- [ ] 7.4 Balancing Autonomy — config for auto-build, slot locking

---

## Phase 8: Ownership & Usage (DEFERRED)
**Goal:** Modules can be owned, affects AI decisions.

- [ ] 8.1 Ownership Assignment — player UI, agent claims on use
- [ ] 8.2 Usage Priority — owner first, others if not claiming
- [ ] 8.3 Mood Integration — own bed buff, stranger's bed debuff

---

## Phase 9: Deconstruction (DEFERRED)
**Goal:** Player can remove modules and structures.

- [ ] 9.1 Mark for Deconstruction — UI button, visual indicator
- [ ] 9.2 Deconstruction Action — action_Deconstruct
- [ ] 9.3 Resource Return — base * HP% * returnPercent
- [ ] 9.4 Structure Deconstruction — modules first, foundation last

---

## Phase 10: Expansion System (DEFERRED)
**Goal:** Structures can be expanded with new wings.

- [ ] 10.1 `ExpansionDefinition` — snapPointIndex, slots[], recipe
- [ ] 10.2 Expansion UI — button, available expansions list
- [ ] 10.3 Expansion Placement — ghost at snap point
- [ ] 10.4 Expansion Construction — same flow, new slots added

---

## Phase 11: Polish & Integration (DEFERRED)
**Goal:** System feels complete, edge cases handled.

- [ ] 11.1 Visual Polish — construction progress, slot highlights, damage
- [ ] 11.2 Audio — construction sounds, completion
- [ ] 11.3 Save/Load — serialize state, rebuild on load
- [ ] 11.4 Debug Tools — inspector, quick-build cheat, visualizer

---

## MVP Milestone (Updated)

**Phases 1-4** = structures can be built by agents ✅
**Phase 4.5** = modules occupy correct slots ⬅️ CURRENT
**Phase 5** = modules built via craft flow

---

## File Locations

```
Building/
├── Data/
│   ├── StructureDefinitionSO.cs
│   ├── ModuleDefinitionSO.cs      ← add slotFootprint
│   ├── SlotDefinition.cs
│   ├── SlotType.cs
│   ├── ConstructionData.cs
│   ├── IConstructionRequirements.cs
│   └── (enums)
├── Runtime/
│   ├── Structure.cs               ← add module placement queries
│   ├── Slot.cs                    ← add multi-slot support
│   ├── Module.cs
│   ├── UnfinishedStructureActor.cs
│   └── (walls, entries)
├── Services/
│   ├── StructuresModule.cs
│   ├── StructurePlacementService.cs
│   ├── StructureConstructionService.cs
│   └── ModulePlacementService.cs  ← NEW
└── Editor/
```

---

*Last updated: Session — Multi-Slot Modules Planning*
