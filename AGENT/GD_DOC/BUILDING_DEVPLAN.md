# Building System — Development Plan

> Extracted from BUILDING.md for tracking implementation progress.

## Status: 🟡 Phase 2.C (Test remaining)

---

## Phase 1: Data Foundation ✅ DONE
**Goal:** Core data structures, no runtime yet.

- [x] 1.1 `StructureDefinitionSO` — scriptable object with footprint, foundationPrefab, slots[], foundationRecipe
- [x] 1.2 `SlotDefinition` — serializable class with slotId, slotType, localPosition/rotation, acceptedModuleTags[], isInterior, startsLocked
- [x] 1.3 `ModuleDefinitionSO` — scriptable object with moduleId, tags[], compatibleSlotTypes[], recipe, prefab, deconstructReturnPercent
- [x] 1.4 `SlotType` enum — Sleeping, Production, Storage, Utility
- [ ] 1.5 Test assets — StructureDefinition_BasicShelter.asset, ModuleDefinition_Bedroll.asset, _Chest.asset

---

## Phase 2: Runtime Structure + Editor Tooling

### 2.A Editor Tooling ✅ DONE

- [x] 2.A.1 `StructureFoundationBuilder.cs` — editor assembly helper
- [x] 2.A.2 `StructureFoundationBuilderEditor.cs` — custom editor with buttons
- [x] 2.A.3 Terrain Check Visualization
- [x] 2.A.4 Slot visualization colors
- [x] 2.A.5 `StructureDescription.cs` — prefab metadata component
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
- [x] 2.C.6 `UnfinishedStructure` (MonoBehaviour) — blueprint + progress

#### Services Layer (DI)
- [x] 2.C.7 `StructurePlacementService` — terrain, ghost
- [x] 2.C.8 `StructureConstructionService` — building logic
- [x] 2.C.9 `StructuresModule` — main coordinator
- [x] 2.C.10 Register services in `GameScope`

- [ ] 2.C.11 Test: full construction flow via services

---

## Phase 3: Placement System
**Goal:** Player can place structure blueprints.

- [ ] 3.1 `StructurePlacementController` — placement mode, ghost preview, grid snap
- [ ] 3.2 Placement Validation — terrain, obstacles, navmesh checks
- [ ] 3.3 Placement Confirmation — calls StructuresModule.PlaceBlueprint()
- [ ] 3.4 Input integration — ESC cancel, R rotate

---

## Phase 4: Construction Flow (GOAP)
**Goal:** Agents build structures and modules.

- [ ] 4.1 Beliefs — UnfinishedStructure_NeedsResources, UnfinishedStructure_NeedsWork
- [ ] 4.2 Actions — action_DeliverResourceToStructure, action_BuildStructure
- [ ] 4.3 Goals — goal_BuildStructure
- [ ] 4.4 Module Building — action_BuildModule after structure complete

---

## Phase 5: Player Module Assignment
**Goal:** Player can assign modules to slots.

- [ ] 5.1 Structure Selection — click to select, info panel
- [ ] 5.2 Slot Visualization — highlight slots, color coding
- [ ] 5.3 Assignment UI — popup with compatible modules
- [ ] 5.4 Priority Control — Low/Normal/High/Critical

---

## Phase 6: Agent Autonomous Building
**Goal:** Agents decide what to build without player input.

- [ ] 6.1 Need Evaluation — map needs to module tags
- [ ] 6.2 Slot Selection — find compatible empty slots
- [ ] 6.3 Auto-Assignment — agent assigns, lower priority than player
- [ ] 6.4 Balancing Autonomy — config for auto-build, slot locking

---

## Phase 7: Ownership & Usage
**Goal:** Modules can be owned, affects AI decisions.

- [ ] 7.1 Ownership Assignment — player UI, agent claims on use
- [ ] 7.2 Usage Priority — owner first, others if not claiming
- [ ] 7.3 Mood Integration — own bed buff, stranger's bed debuff

---

## Phase 8: Deconstruction
**Goal:** Player can remove modules and structures.

- [ ] 8.1 Mark for Deconstruction — UI button, visual indicator
- [ ] 8.2 Deconstruction Action — action_Deconstruct
- [ ] 8.3 Resource Return — base * HP% * returnPercent
- [ ] 8.4 Structure Deconstruction — modules first, foundation last

---

## Phase 9: Expansion System
**Goal:** Structures can be expanded with new wings.

- [ ] 9.1 `ExpansionDefinition` — snapPointIndex, slots[], recipe
- [ ] 9.2 Expansion UI — button, available expansions list
- [ ] 9.3 Expansion Placement — ghost at snap point
- [ ] 9.4 Expansion Construction — same flow, new slots added

---

## Phase 10: Polish & Integration
**Goal:** System feels complete, edge cases handled.

- [ ] 10.1 Visual Polish — construction progress, slot highlights, damage
- [ ] 10.2 Audio — construction sounds, completion
- [ ] 10.3 Save/Load — serialize state, rebuild on load
- [ ] 10.4 Migration — convert CampSetup to StructureDefinitions
- [ ] 10.5 Debug Tools — inspector, quick-build cheat, visualizer

---

## MVP Milestone

Phases 1-4 = minimum playable:
- Player places blueprint (UnfinishedStructure with ghost)
- Agents deliver resources
- Agents do work
- Structure completes (walls, slots, entries)

---

*Last updated: Session with GD — Architecture Refactor*
