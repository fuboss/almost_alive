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
├── Editor/                    # Editor Windows & Wizards
│   ├── TagWizard.cs
│   ├── GOAPFeatureWizard.cs
│   ├── ActorWizard/
│   │   └── ActorIntegrationWizard.cs
│   └── AAWizard/              # 🎯 Unified Wizards Hub
│       ├── AAWizard.cs        # Main Odin window
│       └── RecipeWizard.cs    # Recipe creation wizard
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

## Editor Tools

### AA Wizards Hub 🎯 NEW
**Path:** `Assets/Content/Scripts/Editor/AAWizard/AAWizard.cs`  
**Menu:** AA/Wizards (priority 0)

Unified Odin-powered hub for all AA editor wizards. Single window with tree-view navigation:
- Tag Wizard
- GOAP Feature Wizard
- Actor Integration Wizard
- Recipe Wizard ✨ NEW

Benefits:
- Centralized access to all tools
- Search functionality
- Consistent UI experience
- Easy to extend with new wizards

### Tag Wizard
**Path:** `Assets/Content/Scripts/Editor/TagWizard.cs`  
**Access:** GOAP/Tag Wizard OR AA/Wizards → Tag Wizard

Manages the tag system:
- Create new tags (auto-generates Tag.cs constant + TagDefinition class)
- Delete tags (removes constant + class file)
- Fix incomplete tags (missing constant or class)
- View all tags with status indicators

**Usage:**
1. Open via menu or AA Wizards hub
2. Add tag name (UPPER_SNAKE_CASE)
3. Click Add → auto-creates constant in Tag.cs + XxxTag.cs class
4. Auto-updates Tag.ALL_TAGS array

### GOAP Feature Wizard
**Path:** `Assets/Content/Scripts/Editor/GOAPFeatureWizard.cs`  
**Access:** GOAP/Create Feature OR AA/Wizards → GOAP Feature

Creates GOAP feature structure:
- Creates folder: `Assets/Content/Resources/GOAP/[FeatureName]`
- Creates subfolders: Actions, Beliefs, Goals
- Creates FeatureSet ScriptableObject
- Optionally creates sample goal/belief

### Actor Integration Wizard
**Path:** `Assets/Content/Scripts/Editor/ActorWizard/ActorIntegrationWizard.cs`  
**Access:** GOAP/Actor Integration Wizard OR AA/Wizards → Actor Integration

Fast actor prefab integration workflow:
- Drag&drop source prefab
- Auto-generates actorKey from name (lowercase with underscores)
- Select tags via checkboxes
- Configure ActorDescription (isSelectable, rememberDuration)
- Configure ItemTag properties (weight, stackMax) if ITEM tag selected
- **One-click creation:**
  - Creates prefab in `Assets/Content/Prefabs/Actors/`
  - Adds ActorId + ActorDescription components
  - Adds selected TagDefinition components (auto-configures ItemTag)
  - Registers in Addressables group "Actors" with label "Actors"

**Usage:**
1. Open via menu or AA Wizards hub
2. Drop source prefab → actorKey auto-generates
3. Check desired tags (ITEM auto-enables ItemTag settings panel)
4. Adjust settings if needed
5. Click "Create Actor Prefab" → ready-to-use actor!

**Validation:**
- actorKey must be lowercase with underscores (e.g., `wood_log`, `stone_chunk`)
- Prevents overwriting existing prefabs
- Requires valid source prefab
- Auto-detects and uses existing TagDefinition classes

### Recipe Wizard ✨ NEW
**Path:** `Assets/Content/Scripts/Editor/AAWizard/RecipeWizard.cs`  
**Access:** AA/Wizards → Recipe Wizard

Fast recipe creation workflow for crafting system:
- Select result actor from dropdown (all registered actors)
- Configure basic info (ID, category, display name, icon, priority)
- Set craft settings (time, station type, output count, work required)
- Add required resources (tag + count, dynamic list)
- **One-click creation:**
  - Creates RecipeSO in `Assets/Content/Resources/Recipes/`
  - Configures all recipe data
  - Validates actor existence

**Usage:**
1. Open AA Wizards → Recipe Wizard
2. Enter recipe ID (e.g., `bedroll`, `campfire`)
3. Select result actor from dropdown
4. Configure craft settings:
   - Craft time (seconds to complete)
   - Station type (None/Workbench/Fireplace/Anvil)
   - Output count (items produced per craft)
   - Work required (work units for construction)
5. Add resources:
   - Select tag from dropdown (e.g., WOOD, STONE)
   - Set count needed
   - Click "+ Add Resource" for multiple resources
6. Optional: Set category, display name, icon, build priority
7. Click "Create Recipe" → ready-to-use recipe asset!

**Station Types:**
- None: Craft anywhere by hand
- Workbench: Requires workbench
- Fireplace: Requires fireplace/campfire
- Anvil: Future metalworking (not yet implemented)

**Validation:**
- Recipe ID required and unique
- Result actor must exist in Addressables
- Craft time must be positive
- Output count must be positive
- Prevents overwriting existing recipes

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
Last Updated: 2026-01-27
