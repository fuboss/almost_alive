# AI Engine Notes

> Quick reference for AI systems. See `/AGENT/Docs/` for detailed documentation.

## Paths

| Category | Path |
|----------|------|
| Scripts | `/Assets/Content/Scripts/` |
| Docs (technical) | `/AGENT/Docs/` |
| GD Docs | `/AGENT/GD_DOC/` |

---

## Documentation Index

| File | Topic |
|------|-------|
| ARCHITECTURE.md | Core systems, patterns |
| GOAP.md | Beliefs, actions, planning |
| NAVIGATION.md | NavMesh, stuck detection |
| ANIMALS.md | Animal agents, herding |
| INTERFACE_DECOMPOSITION.md | Agent interfaces |
| WORLD_GENERATION.md | Biomes, terrain, scatters |
| INVENTORY_CRAFT.md | Items, storage, recipes |
| CAMP.md | Camp system (legacy) |
| BUILDING.md (GD_DOC) | Smart Blueprints building system |
| **WFC_BUILDING_CONCEPT.md** | 🆕 WFC процедурная генерация структур |

---

## Agent Interfaces

```
IGoapAgentCore (base)
├── ITransientTargetAgent
├── IInventoryAgent
├── IWorkAgent
├── ICampAgent
└── IHerdMember

IGoapAgent = all above (human)
IGoapAnimalAgent = core + herd (animal)
```

---

## VContainer Modules

```
ActorCreationModule   // Addressables spawn
RecipeModule          // Recipe lookups
CampModule            // Camp instantiation
AnimalsModule         // Herd management
WorldModule           // World generation
```

---

## Current Focus

### WFC Building Generation 🆕
See: `Docs/WFC_BUILDING_CONCEPT.md`

Phase 1 - Foundation:
- [ ] WFCTile, WFCTileSetSO data classes
- [ ] WFCSimpleSolver (footprint generation)
- [ ] Socket system for walls
- [ ] Editor preview tool

### Building System (Smart Blueprints)
See: `GD_DOC/BUILDING.md`, `GD_DOC/BUILDING_DEVPLAN.md`

Core complete:
- [x] StructureDefinitionSO
- [x] SlotDefinition
- [x] ModuleDefinitionSO
- [x] SlotType enum

### Deferred

- [ ] VisionSensor ↔ WorldEnvironment.visionModifier
- [ ] World Gen neighbors system
- [ ] Temperature system (IDEAS_NIKITA.md)
- [ ] LocomotionLayer for agent local behaviors

---

*Update when major systems change.*
