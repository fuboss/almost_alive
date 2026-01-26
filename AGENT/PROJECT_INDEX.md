# Project Index - Almost Alive

## Project Overview
**Type:** 3D Colony Simulation (RimWorld-inspired)
**Engine:** Unity 2022+
**Stack:** C# + VContainer + Odin + DOTween + UniTask + Addressables

## Directory Structure

```
Assets/Content/Scripts/
├── AI/                        # AI systems (GOAP, Navigation, Craft)
├── Building/                  # Building system
│   ├── Data/                  # ScriptableObjects, data structures
│   │   ├── Expansion/         # Expansion system (SnapPoint, StructureConnection)
│   │   ├── StructureDefinitionSO.cs
│   │   ├── ModuleDefinitionSO.cs
│   │   └── BuildingConstants.cs
│   ├── Runtime/               # Runtime components
│   │   ├── Visuals/           # Visual management (decorations)
│   │   ├── Structure.cs
│   │   ├── UnfinishedStructureActor.cs
│   │   ├── Slot.cs
│   │   └── Module.cs
│   ├── Services/              # Services (stateless logic)
│   │   ├── Visuals/           # Visual strategies
│   │   ├── StructurePlacementService.cs
│   │   ├── StructureConstructionService.cs
│   │   └── StructureExpansionService.cs
│   └── Editor/                # Editor tools
├── Core/                      # Core systems
├── Descriptors/               # Actor/Tag descriptors
├── Game/                      # Game-level systems
│   └── Craft/                 # Crafting system
└── World/                     # World systems
    └── Grid/                  # WorldGrid, GroundCoord
        ├── WorldGrid.cs       # Spatial index (static)
        ├── GroundCoord.cs     # 2D grid coordinate struct
        └── Presentation/      # Grid visualization
            ├── WorldGridPresentationModule.cs
            ├── WorldGridPresentationConfigSO.cs
            ├── DecalGridVisualizer.cs (URP Decal)
            └── LineRendererGridVisualizer.cs (Fallback)

```

## Dependency Injection (VContainer)

**ROOT SCOPE:** `Assets/Resources/GameScope.prefab`  
**SCRIPT:** `Assets/Content/Scripts/GameScope.cs`

### Registration Pattern

```csharp
protected override void Configure(IContainerBuilder builder) {
    // Singleton service
    builder.Register<MyService>(Lifetime.Singleton).AsSelf();
    
    // Interface + Self
    builder.Register<MyModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
    
    // EntryPoint (ITickable/IStartable)
    builder.RegisterEntryPoint<MyTickableModule>().AsSelf();
    
    // Instance (SO/config)
    builder.RegisterInstance(myConfig).AsSelf();
    
    // Keyed dependency
    builder.RegisterInstance(material).Keyed("ghostMaterial").AsSelf();
}
```

### Adding New Services

1. Open `GameScope.cs`
2. Add using for your service namespace
3. Add registration in appropriate section:
   - **Simulation** - SimulationLoop, TimeController
   - **Environment** - World, Navigation, Trees, Animals
   - **Building** - Structure services
   - **AI** - GOAP, Agents
   - **UI** - UI modules

**Example (Building Service):**
```csharp
// Building section
builder.Register<StructureVisualsModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
builder.Register<LinearProgressionStrategy>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
```

## Building System Architecture

### Core Types

**Structure Types:**
- `Enclosed` - Full structure with walls, roof, supports, entries
- `Open` - Outdoor structures without walls (camps, plazas)

**Structure States:**
```csharp
enum StructureState {
    BLUEPRINT,
    UNDER_CONSTRUCTION,
    BUILT,
    DAMAGED,
    DESTROYED
}
```

### Services

| Service | Purpose |
|---------|---------|
| StructurePlacementService | Terrain positioning, ghost preview |
| StructureConstructionService | Build walls, supports, entries, slots |
| StructureExpansionService | Snap points, expansion connections |
| ModulePlacementService | Place modules in structure slots |
| StructureVisualsModule | Decoration visibility management |

### Key Components

**Structure.cs** - Runtime structure instance
- Slots management
- Module assignment
- Wall/Entry/Support containers
- Expansion connections

**UnfinishedStructureActor.cs** - Under construction
- Work progress tracking
- Resource delivery
- Ghost management
- Completion logic

**StructureDecoration.cs** - Decoration visibility
- Visibility modes (Always, OnConstruction, AfterCoreModule, WithModule)
- Animation support
- Context-based evaluation

### Slot System

**SlotType:** Interior, Exterior, Wall, Roof, Foundation
**SlotPriority:** LOW, NORMAL, HIGH

Module placement validates:
- Footprint compatibility
- Clearance radius
- Core module requirement
- Tag matching

## Recent Features

### WorldGrid Presentation System (Jan 2026) ⏳
- In-game grid visualization for debugging/placement
- Dual rendering backends: URP Decal (primary) + LineRenderer (fallback)
- Mode system: Hidden, StaticGrid, PlacementPreview
- Integration with DebugPanel for actor/structure placement preview
- Configurable via ScriptableObject
- See: `/AGENT/Features/WORLDGRID_PRESENTATION.md`

### WFC Building Generation (Jan 2026) 🟡
- Procedural structure generation via Wave Function Collapse
- Non-rectangular footprints using WorldGrid
- Modular tile system with socket constraints
- Multi-floor support with MaterialSets
- See: `/AGENT/Docs/WFC_BUILDING_CONCEPT.md`

### Expansion System (Jan 2026)
- SnapPoint-based attachment
- Wall-to-passage conversion
- Entry removal at connections
- Navigation link updates

### Visual Management (Jan 2026)
- Strategy pattern for animations/progression
- Dirty tracking optimization
- Module-dependent decorations
- Construction-progressive reveal
- Ghost auto-disable on work start

### Open Structures (Jan 2026)
- TerrainSnapDecoration component
- Terrain-following decorations
- No walls/supports generation

## Code Style

### Patterns
- **Services:** Stateless, injected dependencies
- **Components:** MonoBehaviour data containers
- **Strategies:** Interface-based extensibility
- **Modules:** Singleton services with ITickable

### Naming
- Private fields: `_camelCase`
- Public properties: `camelCase`
- Methods: `PascalCase`
- Constants: `PascalCase`

### Documentation
- XML summary on public APIs
- Odin attributes for Inspector
- Tooltips on serialized fields

## TODO: Missing Documentation

1. **DI Registration**
   - Root scope location
   - Service registration pattern
   - How to add new services

2. **Game Loop**
   - Main loop structure
   - Tick order
   - Fixed vs variable update

3. **Actor System**
   - ActorRegistry pattern
   - ActorDescription lifecycle
   - Tag system

4. **Navigation**
   - NavMesh generation flow
   - NavigationModule API
   - Surface registration

5. **Resource System**
   - Item tags
   - Inventory system
   - Resource delivery

## Contact
Project: Almost Alive
Developer: Nikita (13+ years Unity exp)
Last Updated: 2026-01-26
