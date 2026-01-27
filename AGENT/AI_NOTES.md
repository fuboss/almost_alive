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
| **WFC_BUILDING_CONCEPT.md** | WFC процедурная генерация структур |
| **UI.md (GD_DOC)** | 🆕 UI Layout & Inspector design |

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

### UI System — Inspector for AI Debug 🆕
See: `GD_DOC/UI.md`

Design approved. Key decisions:
- **Right panel inspector** (304px) with tabs
- **Tab: Plan** — Goal → Actions → Related Stats/Beliefs (PRIMARY)
- **Tab: Needs & Goals** — All stats + available goals with priorities
- **Tab: Beliefs** — Full beliefs dump
- **Camera modes** — Free + Follow (via CameraModule API)
- Single select now, architecture for multi-select

Existing classes to extend:
- `MainInfoPanel` (has tab system)
- `BaseInfoPanel` (abstract for tabs)
- `StatsPanel`, `DebugPanel` (examples)

New classes needed:
- [x] `PlanPanel : BaseInfoPanel` ✅
- [ ] `NeedsGoalsPanel : BaseInfoPanel`  
- [x] `SelectionService` (implements IAgentSelectionModule) ✅
- [x] `SelectionInputHandler` ✅
- [x] `PlanActionItem` ✅
- [x] `CameraModeWidget` ✅
- [x] `InspectorLayer` + `IInspectorView` ✅
- [x] `AgentInspectorView` ✅
- [x] `ActorInspectorView` ✅
- [x] `TopBarLayer` ✅
- [x] `ResourcePanelWidget` ✅
- [x] `ContextActionRegistry` + `WorkMarker` ✅
- [x] `BottomBarLayer` + command system ✅
- [x] `DebugCommandsRegistrar` ✅
- [x] `ColonyProgressionModule` + `ColonyProgressionConfigSO` ✅
- [x] `BuildCommandsRegistrar` (from RecipeSO) ✅
- [x] `DebugPanelUI` nested menus via `/` in displayName ✅

### Progression System 🆕
See: `Game/Progression/`

- `ColonyProgressionConfigSO` — what's unlocked at each milestone
- `ColonyProgressionModule` — tracks unlocked recipes, research
- `BuildCommandsRegistrar` — fills Build menu from unlocked recipes
- Future: research tree, events, achievements

### WFC Building Generation
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
- [ ] Notifications system (foundation laid in UI.md)

---

## UI Key Data for Inspector

What to display per tab:

**Plan Tab:**
```csharp
agent.agentBrain.currentGoal        // AgentGoal
agent.agentBrain.actionPlan         // ActionPlan (Stack<AgentAction>)
agent.agentBrain.actionPlan.actions // current plan actions
agent.body.GetStatsInfo()           // related stats
agent.agentBrain.beliefs            // filter by relevance to current goal
```

**Needs & Goals Tab:**
```csharp
agent.body.GetStatsInfo()           // all FloatAgentStat
agent.agentBrain.goalTemplates      // all available goals
// need to expose calculated priorities per goal
```

**Beliefs Tab:**
```csharp
agent.agentBrain.beliefs            // Dictionary<string, AgentBelief>
belief.lastEvaluation               // bool
```

---

*Update when major systems change.*
