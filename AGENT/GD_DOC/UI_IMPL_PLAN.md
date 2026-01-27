# UI Implementation Plan

## Architecture

```
SelectionService (IAgentSelectionModule)
    ↓ OnSelected event
InspectorLayer (UILayer)
    ↓ switches between views
├── AgentInspectorView (IInspectorView) — for GOAPAgent
│   └── tabs: PlanPanel, NeedsPanel, BeliefsPanel
└── ActorInspectorView (IInspectorView) — for trees, rocks, items
    └── header + context actions

TopBarLayer (UILayer)
├── Time display
├── CameraModeWidget (Free/Follow)
└── ResourcePanelWidget (extensible)

BottomBarLayer (UILayer)
├── CommandCategoryButton[] — Build, Orders, Work, Zones, Debug, Menu
└── CommandSubmenu — popup with CommandMenuItem[]

CommandRegistry (static)
    ↓ GetByCategory(category)
DebugCommandsRegistrar — bridges DebugModule → CommandRegistry

ContextActionRegistry
    ↓ GetActionsFor(actor)
WorkContextActionsRegistrar — registers "Mark for Chopping" etc.

WorldGridPresentationModule
    ↓ OnSelectionChanged
SelectionVisualizer — highlights cell under selected actor
```

## Completed ✅

### Selection System
- [x] `ISelectable` extends `ISelectableActor`
- [x] `SelectionService` — works with any `ISelectableActor`
- [x] `SelectionInputHandler` — click raycast, ignores UI
- [x] Selection → WorldGrid highlight integration

### Inspector System
- [x] `IInspectorView` interface
- [x] `InspectorLayer` — switches views based on actor type
- [x] `AgentInspectorView` — for GOAPAgent, tabs system
- [x] `ActorInspectorView` — for generic actors (trees, resources)
- [x] `PlanPanel` — Goal + Actions + Stats + Beliefs
- [x] `PlanActionItem` — action list item

### TopBar
- [x] `TopBarLayer` — time display
- [x] `CameraModeWidget` — Free/Follow toggle
- [x] `ResourcePanelWidget` — extensible resource display
- [x] `ResourceEntryWidget` — single resource item

### BottomBar
- [x] `ICommand` interface + `Command` class
- [x] `CommandCategory` enum (Build, Orders, Work, Zones, Debug, Menu)
- [x] `CommandRegistry` — static registry with events
- [x] `BottomBarLayer` — category buttons + submenu
- [x] `CommandCategoryButton` — category button with selection state
- [x] `CommandSubmenu` — popup positioned above button
- [x] `CommandMenuItem` — single command in submenu
- [x] `DebugCommandsRegistrar` — bridges DebugModule actions

### Context Actions
- [x] `IContextAction` interface
- [x] `ContextAction` — lambda-based implementation
- [x] `ContextActionRegistry` — tag-based action lookup
- [x] `ContextActionButton` — UI button
- [x] `WorkContextActionsRegistrar` — registers work actions
- [x] `WorkMarker` — marks actors for work

## Unity Setup Required

### Prefabs to create:
1. **TopBarLayer** prefab
   - Add to `uiLayers` array in GameScope
   - Wire CameraModeWidget, ResourcePanelWidget

2. **InspectorLayer** prefab  
   - Add AgentInspectorView, ActorInspectorView as children
   - Wire `_viewComponents` list (order matters!)
   - Add to `uiLayers` array

3. **BottomBarLayer** prefab
   - Left container + Right container
   - CommandCategoryButton prefab
   - CommandSubmenu child with CommandMenuItem prefab
   - Configure `_categoryConfigs`:
     - Build (🏠), Orders (⚙️), Work (📋), Zones (🗺️), Debug (⚡) — left
     - Menu (≡) — right

4. **ActorInspectorView** prefab
   - Header: icon, name, description, tags
   - Actions container + ContextActionButton prefab

5. **ResourceEntryWidget** prefab
   - Icon + count text

6. **ContextActionButton** prefab
   - Button + label + icon

### GameScope changes:
- Add InspectorLayer, TopBarLayer, BottomBarLayer to `uiLayers[]`

## Command System Usage

```csharp
// Register a command (from any module)
CommandRegistry.Register(new Command(
    id: "build.wall",
    label: "Build Wall",
    icon: "🧱",
    category: CommandCategory.Build,
    execute: () => StartWallPlacement(),
    canExecute: () => HasResources("wood", 5),
    order: 10
));

// Unregister when module unloads
CommandRegistry.Unregister("build.wall");
```

## Data Flow

```
Click on actor
    ↓
SelectionInputHandler.Tick()
    ↓ Physics.Raycast → GetComponentInParent<ISelectableActor>
SelectionService.Select(actor)
    ↓ OnSelected event
├── InspectorLayer.OnSelectionChanged()
│   ↓ finds matching IInspectorView
│   AgentInspectorView.Show(agent) OR ActorInspectorView.Show(actor)
└── WorldGridPresentationModule.OnSelectionChanged()
    ↓ UpdateSelectionHighlight()
    _selectionRenderer.ShowTile(coord, color)
```

```
Click BottomBar category
    ↓
BottomBarLayer.OnCategoryClicked()
    ↓
CommandRegistry.GetByCategory(category)
    ↓
CommandSubmenu.Show(commands, anchorButton)
    ↓
User clicks command
    ↓
command.Execute()
```

### Progression System
- [x] `ColonyProgressionConfigSO` — milestones + recipe unlocks
- [x] `ColonyProgressionModule` — tracks current milestone, unlocked recipes
- [x] `BuildCommandsRegistrar` — populates Build category from unlocked recipes

### DebugPanel Hierarchical Menus
- [x] Supports nested menus via `/` in displayName
- [x] Example: `"Time/Dawn (06:00)"`, `"Fireplace (2x2)/Instant Spawn"`
- [x] Back button navigation
- [x] Breadcrumb display

## Next Steps

- [ ] `NeedsGoalsPanel` — all stats + goals with priorities
- [ ] Visual indicators for WorkMarker (outline, icon)
- [ ] Double-click to follow with camera
- [ ] Orders menu (haul, clean, etc.)
- [ ] Work priorities panel
- [ ] Zones system foundation
- [ ] Research system (unlocks via ColonyProgressionModule)
