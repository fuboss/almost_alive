# AI Engine Notes

> Quick reference for AI systems. See `/AGENT/Docs/` for detailed documentation.

## Paths

| Category | Path |
|----------|------|
| Scripts | `/Assets/Content/Scripts/` |
| Docs (technical) | `/AGENT/Docs/` |
| GD Docs | `/AGENT/GD_DOC/` |

---

## Water System Overview

**Water Level Sync**: TerrainSculptPhase syncs with scene `WaterPlane` object

### Lake Biomes (BiomeSO)
```
isWaterBody = true
waterDepth: 0.5-15m (depth at center)
shoreGradient: 0-1 (0=steep, 1=gradual)
```

### River Shore Styles (per BiomeSO)
```csharp
enum RiverShoreStyle {
  Natural,   // Standard smoothstep
  Soft,      // Beach-like, double smoothstep
  Rocky,     // Sharp cliffs + noise irregularity
  Marshy,    // Very gradual, extended wet zone
  Terraced   // Step-like geological profile
}

// BiomeSO fields:
riverShoreStyle      // Shore type
riverShoreGradient   // Slope steepness 0-1
riverShoreWidth      // Transition zone (1-15m)
rockyIrregularity    // Noise for rocky edges (0-1)
```

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
| **WORLD_GENERATION_PIPELINE.md** | Phased generation, Artist Mode, Water System ✅ |
| **ARTIST_MODE_WINDOW_REFACTOR.md** | ✅ ArtistModeWindow SOLID refactor (done) |
| INVENTORY_CRAFT.md | Items, storage, recipes |
| CAMP.md | Camp system (legacy) |
| BUILDING.md (GD_DOC) | Smart Blueprints building system |
| **WFC_BUILDING_CONCEPT.md** | WFC процедурная генерация структур |
| **UI.md (GD_DOC)** | UI Layout & Inspector design |

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

## Editor Wizards

| Wizard | Path | Purpose |
|--------|------|--------|
| AAWizard | `Editor/AAWizard/` | Tag, GOAP Features, Actor Integration, Recipes |
| **WorldGenerationWizard** | `Editor/WorldGenerationWizard/` | 🆕 Biomes, Scatters, Vegetation databases |

### WorldGenerationWizard Pages

1. **Generation Config** — WorldGeneratorConfigSO inline editor + quick actions (generate/clear/preview)
2. **Biomes Database** — TableList of all BiomeSO + create/edit/delete
3. **Scatters Database** — TableList of all ScatterRuleSO + create/edit/delete  
4. **Vegetation Database** — TableList of all VegetationPrototypeSO + create/edit/delete

**Pattern**: `[Serializable]` composites + Odin attributes (no EditorGUILayout)
- `[TableList]` for asset lists
- `[InlineEditor]` for selected asset editing
- `[Button]`, `[EnableIf]`, `[ShowIf]` for actions
- Entry classes with table columns + edit/delete buttons

---

## Current Focus

### World Generation Pipeline ✅ Core Done
See: `Docs/WORLD_GENERATION_PIPELINE.md`

**Completed:**
- [x] Noise System (6 samplers + 3 modifiers + 3 combinators)
- [x] Pipeline Core (IGenerationPhase, GenerationContext, GenerationPipeline)
- [x] All 5 Phases (BiomeLayout, TerrainSculpt, SplatmapPaint, Vegetation, Scatter)
- [x] ScriptableConfig Refactor — data in structs
- [x] ArtistModeWindow.cs (dockable EditorWindow)
- [x] Debug visualization (Quad overlay + BiomeGizmoDrawer)
- [x] Domain Warping (organic biome borders)
- [x] Context-sensitive phase settings in ArtistModeWindow
- [x] **SOLID Refactor** — ArtistModeWindow decomposed into Drawers + State + PhaseSettings
- [x] **Terrain Sculpt expanded** — global noise, slope limiting, river carving

**Architecture:**
```
Editor/WorldGenerationWizard/
├── ArtistModeWindow.cs              // 5KB coordinator
└── ArtistMode/
    ├── ArtistModeStyles.cs          // Shared GUI styles
    ├── ArtistModeState.cs           // State + pipeline logic
    ├── Drawers/                     // Section drawers
    │   ├── HeaderDrawer.cs
    │   ├── ConfigDrawer.cs
    │   ├── SeedDrawer.cs
    │   ├── PhasesListDrawer.cs
    │   ├── ActionsDrawer.cs
    │   └── StatusDrawer.cs
    └── PhaseSettings/               // Per-phase settings
        ├── IPhaseSettingsDrawer.cs
        ├── BiomeLayoutSettingsDrawer.cs
        ├── TerrainSculptSettingsDrawer.cs
        └── ...
```

**TerrainSculptPhase Features:**
- Global noise (large hills + fine detail)
- Slope limiting (for NavMesh compatibility)
- River carving along biome borders

**TODO:**
- [ ] Debug shaders (HeightGradient, DensityHeatmap) - optional
- [ ] ⏸️ Preset system (отложено)

**Scatter Phase Fixes (Jan 2026):**
- ✅ Removed prefab field from ScatterRuleSO (was added by mistake)
- ✅ ScatterPhase now loads actors via Addressables by actorKey (sync load via WaitForCompletion)
- ✅ ScatterSettingsDrawer validates actorKey against Addressables 'Actors' label
- ✅ Added per-rule spawn statistics logging
- ✅ CleanupExistingScatters() — removes [Generated_Scatters] before creating new
- ✅ GenerationPipeline.RerunPhase() — allows re-running specific phase without full restart
- ✅ Phase-specific deterministic random via CreatePhaseRandom(phaseIndex)
- ✅ Biome containers for hierarchy organization ([BiomeType] folders under [Generated_Scatters])
- ⚠️ **Important**: ScatterRuleSO.actorKey MUST match ActorDescription.actorKey in Addressables

**WorldGeneratorEditor Refactor (Jan 2026):**
- ✅ Now uses GenerationPipeline instead of old EditorWorldGenerator
- ✅ Same results as ArtistModeWindow (consistent generation)
- ✅ Menu: World/Generate (Edit Mode), World/Clear Generated
- ✅ SaveToDevPreloadWorld collects spawn data from scene hierarchy

---

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

### Harvestable Plants System ✅
Path: `Game/Harvesting/`

**Status:** Core implementation done

**Components:**
- `HarvestableTag` — настройки (actorKey, maxHarvest, respawnTime, workPerUnit, curve)
- `GrowthProgress : ISimulatable` — симуляция роста (progress 0-1 → currentYield)
- `HarvestingProgress` — прогресс работы агента (как ChoppingProgress)
- `HarvestModule` — менеджер (init, spawn yield, static helpers)
- `HarvestStrategy` — GOAP strategy (work → drop on ground)
- `TreeTag` — регистрируется в ActorRegistry (для "Chop All Trees")
- `HarvestableHasYieldBelief` — belief для проверки урожая

**GOAP Flow:**
```
MoveToHarvestable (MoveStrategy + MemorySearcher[HARVESTABLE])
  → HarvestFromPlant (HarvestStrategy)
    → yield drops on ground
      → PickupItem (separate action)
```

**Architecture:**
- SOLID: GrowthProgress ticks progress, HarvestModule converts to yield
- Registration via ActorRegistry<HarvestableTag>
- VContainer: HarvestModule in GameScope

**Beliefs:**
- `HarvestableHasYieldBelief` — transient target has yield
- `HarvestableInMemoryBelief` — memory has harvestable with yield (+ distance check)

**Work System Integration:**
- `WorkType.FARMING` — check via `HasFarmingWork()`
- Context actions:
  - 🪓 "Chop Tree" / 🌲 "Chop All Trees"
  - ⛏️ "Mine Rock"
  - 🌿 "Harvest" / 🧺 "Harvest All Ready"
  - ❌ "Cancel Work"

**TODO:**
- [ ] View для визуала плодов (позже)
- [ ] GOAP Action SO (создать в редакторе)
- [ ] Тест с реальным кустом

---

*Update when major systems change.*
