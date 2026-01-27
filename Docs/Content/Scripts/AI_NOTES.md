# AI Engine Notes

Last-Updated: 2026-01-27

## Session: 2025-01-25 — Multi-Slot Modules + Construction Flow ✅

### Phase 4.5 — Multi-Slot Modules
- `ModuleDefinitionSO`: slotFootprint, clearanceRadius, removed prefab
- `StructureDefinitionSO`: coreModule, coreModuleSlotIds
- `Slot`: OCCUPIED state, anchorSlot, multi-slot methods
- `Structure`: FindSlotsForModule, ValidateClearance, isCoreBuilt
- `ModulePlacementService`: PlaceModule, PlaceCoreModule, RemoveModule
- `PlaceModuleAction`: DebugPanel integration

### Phase 5 — Module Construction Flow
- `UnfinishedModuleActor`: extends UnfinishedActorBase, stores targetStructure/anchorSlot
- `ModulePlacementService`: AssignModule → UnfinishedModuleActor, CompleteModule, InstantPlaceModule
- `AssignModuleAction`: DebugPanel action for construction flow
- Auto-integrates with UnfinishedQuery (same ActorRegistry)

### Key Design
1. **CoreModule**: Structure blocks other modules until core built
2. **Clearance**: Ring around module must be free (or outside bounds)
3. **Construction Flow**: AssignModule → UnfinishedModuleActor → agents work → CompleteModule
4. **Instant placement**: For debug/cheats, bypasses construction

### Required Prefab
- `unfinished_module` with ActorDescription, ActorInventory, UnfinishedModuleActor
- Addressable label: "Actor"

---

## Paths
- **Scripts:** `/Assets/Content/Scripts/`
- **Docs:** `/Assets/Content/Scripts/Docs/`
- **GD Docs:** `/AGENT/GD_DOC/`

---

## Docs

| File                       | Topic                                   |
|----------------------------|-----------------------------------------|
| ARCHITECTURE.md            | Core systems, patterns                  |
| GOAP.md                    | Beliefs, actions, planning              |
| NAVIGATION.md              | NavMesh, stuck detection, interruptions |
| ANIMALS.md                 | Animal agents, herding                  |
| INTERFACE_DECOMPOSITION.md | Agent interfaces                        |
| WORLD_GENERATION.md        | Biomes, terrain, scatters, vegetation   |
| INVENTORY_CRAFT.md         | Items, storage, recipes                 |
| CAMP.md                    | Camp system (DEPRECATED)                |
| BUILDING_DEVPLAN.md        | Building system dev plan                |
| REFACTORING_IDEAS.md       | pending refactoring                     |

---

## Interfaces

```
IGoapAgentCore (base)
├── ITransientTargetAgent
├── IInventoryAgent
├── IWorkAgent
├── IBuilderAgent       ← structures
├── ICampAgent          ← deprecated
└── IHerdMember

IGoapAgent = all above (human)
IGoapAnimalAgent = core + herd (animal)
```

---

## Modules (VContainer)

```
ActorCreationModule       // Addressables spawn
ActorDestructionModule    // Actor destruction
RecipeModule              // Recipe lookups
CampModule                // Camp (DEPRECATED)
AnimalsModule             // Herd management
WorldModule               // World generation
StructuresModule          // Structure CRUD, placement
NavigationModule          // NavMesh baking
```

---

## Building System Architecture

```
StructuresModule (coordinator)
├── StructurePlacementService (terrain, ghost)
├── StructureConstructionService (walls, slots, entries)
└── ModulePlacementService (TODO — module placement)

Runtime:
├── Structure (built structure with slots)
├── Slot (slot state, module reference)
├── Module (runtime module instance)
├── UnfinishedStructureActor (blueprint being built)
└── WallSegment, EntryPoint (structure parts)
```

---

## TODO

### Current: Building Phase 4.5
- [ ] ModuleDefinitionSO.slotFootprint
- [ ] Slot multi-occupancy logic
- [ ] Structure.FindSlotsForModule()
- [ ] ModulePlacementService
- [ ] DebugPanel module placement

### Next: Phase 5 Module Construction
- [ ] UnfinishedModuleActor
- [ ] Craft flow for modules

### Deferred
- [ ] Player UI for module assignment
- [ ] Agent autonomous building
- [ ] Module ownership
