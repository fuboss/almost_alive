# Building System — Development Plan

> Extracted from BUILDING.md for tracking implementation progress.

## Status: 🟡 Phase 1

---

## Phase 1: Data Foundation ⬅️ CURRENT
**Goal:** Core data structures, no runtime yet.

- [x] 1.1 `StructureDefinitionSO` — scriptable object with footprint, foundationPrefab, slots[], foundationRecipe
- [x] 1.2 `SlotDefinition` — serializable class with slotId, slotType, localPosition/rotation, acceptedModuleTags[], isInterior, startsLocked
- [x] 1.3 `ModuleDefinitionSO` — scriptable object with moduleId, tags[], compatibleSlotTypes[], recipe, prefab, deconstructReturnPercent
- [x] 1.4 `SlotType` enum — Sleeping, Production, Storage, Utility
- [ ] 1.5 Test assets — StructureDefinition_BasicShelter.asset, ModuleDefinition_Bedroll.asset, _Chest.asset

---

## Phase 2: Runtime Structure
**Goal:** Structures exist in world, no construction yet.

- [ ] 2.1 `Structure` (MonoBehaviour) — ref to SO, runtime slots[], state machine, HP
- [ ] 2.2 `Slot` (class) — runtime state, assignedModuleDef, builtModule, priority, owner
- [ ] 2.3 `Module` (MonoBehaviour) — ref to SO, owner, HP, CanUse()
- [ ] 2.4 `StructureRegistry` — static Registry<Structure> pattern
- [ ] 2.5 Test: spawn Structure via code, verify slots initialize

---

## Phase 3: Placement System
**Goal:** Player can place structure blueprints.

- [ ] 3.1 `StructurePlacementController` — placement mode, ghost preview, grid snap
- [ ] 3.2 Placement Validation — terrain, obstacles, navmesh checks
- [ ] 3.3 Placement Confirmation — spawn Structure in Blueprint state
- [ ] 3.4 Input integration — ESC cancel, R rotate

---

## Phase 4: Construction Flow
**Goal:** Agents build structures and modules.

- [ ] 4.1 `StructureConstructionSite` — resource tracking, progress
- [ ] 4.2 Resource Delivery — hauling to blueprint site
- [ ] 4.3 Foundation Building — action_BuildFoundation, single builder
- [ ] 4.4 Module Building — action_BuildModule, slot resources
- [ ] 4.5 Beliefs & Goals — Structure_NeedsFoundation, Structure_NeedsModule, goal_BuildStructure

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
- Player places blueprint
- Agents build foundation
- Modules appear in slots

---

*Last updated: Session with GD*
