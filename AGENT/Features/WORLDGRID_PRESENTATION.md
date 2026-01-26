# WorldGrid Presentation Module

**Feature Status:** Planning  
**Created:** 2026-01-26  
**Last Updated:** 2026-01-26

## Overview

Visualization system for WorldGrid - displays grid cells in-game and highlights hover/placement areas when using DebugPanel.

## Requirements

### Core Features
1. **Global Grid Toggle** - UI button to show/hide grid overlay
2. **Hover Highlight** - Show cell under mouse when placing actors/structures
3. **Footprint Preview** - Highlight all cells covered by structure footprint
4. **Visual Feedback** - Color-coded valid/invalid placement zones

### Integration Points
- **WorldGrid** - spatial indexing system (cellSize = 1.0)
- **DebugModule** - state management (Idle, Browsing, ReadyToApply)
- **DebugPanelUI** - UI layer with toggle button
- **Actions** - SpawnActorAction, SpawnStructureAction, PlaceModuleAction

## Technical Design

### Architecture

```
WorldGridPresentationModule (Singleton Service)
├── IGridVisualizer (Decal-based grid rendering)
├── IHoverVisualizer (Single cell highlight)
├── IFootprintVisualizer (Multi-cell structure preview)
└── ISlotVisualizer (Future: structure slot display)
```

**Mode System:**
```csharp
enum GridVisualizationMode {
  Hidden,            // Nothing visible
  StaticGrid,        // Grid lines only
  PlacementPreview,  // Grid + hover/footprint
  SlotPreview,       // Structure slots (future)
  DensityHeatmap,    // Actor density (future)
  NavigationCost     // Nav cost overlay (future)
}
```

### Key Components

#### 1. WorldGridPresentationModule
**Role:** Main service, orchestrates grid visualization

```csharp
public class WorldGridPresentationModule : IStartable, ILateTickable {
  [Inject] private DebugModule _debugModule;
  
  private GridRenderer _gridRenderer;
  private HoverHighlighter _hoverHighlighter;
  private FootprintPreview _footprintPreview;
  
  private bool _isGridVisible;
  private GridDisplayMode _currentMode = GridDisplayMode.Hidden;
  
  public void SetGridVisibility(bool visible);
  public void SetDisplayMode(GridDisplayMode mode);
  void ILateTickable.LateTick(); // Update hover/preview
}
```

**Display Modes:**
- `Hidden` - Grid completely hidden
- `StaticGrid` - Only show grid lines (subtle, for orientation)
- `InteractionMode` - Grid + hover highlight (when placing objects)

#### 2. DecalGridVisualizer
**Role:** Draw grid using URP Decal Projector

**Why Decal:**
- Automatically follows terrain height variations
- One draw call for entire visible area
- Clean URP integration
- Easy fade in/out via shader properties

**Implementation:**
- Decal Projector positioned above camera view
- Projects downward onto terrain/structures
- Grid pattern generated procedurally in shader (UV-based)
- Culling based on camera frustum

```csharp
public interface IGridVisualizer {
  void Show();
  void Hide();
  void SetIntensity(float alpha);
}

public class DecalGridVisualizer : MonoBehaviour, IGridVisualizer {
  private DecalProjector _decalProjector;
  private Material _gridMaterial; // Instanced
  
  private static readonly int AlphaProperty = Shader.PropertyToID("_Alpha");
  private static readonly int CellSizeProperty = Shader.PropertyToID("_CellSize");
  
  public void Show();
  public void Hide();
  public void SetIntensity(float alpha);
  public void UpdateProjection(Camera camera, float renderDistance);
}
```

#### 3. HoverVisualizer
**Role:** Highlight single cell under mouse

```csharp
public interface IHoverVisualizer {
  void ShowHover(GroundCoord coord, bool isValid);
  void Hide();
}

public class HoverVisualizer : MonoBehaviour, IHoverVisualizer {
  private MeshRenderer _highlightQuad;
  private Material _hoverMaterial; // Instanced
  
  private float _pulseTime;
  
  public void ShowHover(GroundCoord coord, bool isValid);
  public void Hide();
  public void UpdatePulse(float deltaTime, WorldGridPresentationConfigSO config);
}
```

#### 4. FootprintVisualizer
**Role:** Multi-cell structure placement preview

```csharp
public interface IFootprintVisualizer {
  void ShowFootprint(GroundCoord origin, Vector2Int footprint, bool isValid);
  void Hide();
}

public class FootprintVisualizer : MonoBehaviour, IFootprintVisualizer {
  private List<MeshRenderer> _cellQuads; // Pooled
  private Material _previewMaterial; // Instanced
  
  public void ShowFootprint(GroundCoord origin, Vector2Int footprint, bool isValid);
  public void Hide();
  public void EnsureQuadPool(int requiredCount);
}
```

### UI Integration

#### DebugPanelUI Extension
Add toggle button to existing DebugPanelUI:

```csharp
private Toggle _gridToggle;

private void BuildUI() {
  // ... existing code ...
  
  // Add Grid Toggle (left side of status text)
  var toggleObj = CreateToggle("GridToggle", _contentPanel.transform, "Grid", OnGridToggled);
  // Position before status text
}

private void OnGridToggled(bool isOn) {
  _debugModule.gridPresentation.SetGridVisibility(isOn);
}
```

### State Management

**Flow Diagram:**
```
User opens DebugPanel (F12)
  ├─> State: Browsing
  ├─> Grid: Hidden by default
  └─> User toggles grid → Grid: StaticGrid

User selects "Spawn Actor"
  ├─> State: ReadyToApply
  ├─> Grid: InteractionMode (auto-enable)
  ├─> HoverHighlighter: Active (single cell)
  └─> User clicks → Spawn & reset

User selects "Place Structure (3x3)"
  ├─> State: ReadyToApply
  ├─> Grid: InteractionMode
  ├─> FootprintPreview: Active (3x3 cells)
  └─> Validate placement (terrain height, overlaps)
```

### Placement Validation

**For Actors:**
- Terrain raycast successful
- (Optional) Cell not occupied

**For Structures:**
- All footprint cells on valid terrain
- Height variance within threshold (StructurePlacementService logic)
- (Optional) No overlapping structures

**For Modules:**
- Target structure selected
- Valid slot available
- Clearance radius check

## Implementation Plan

### Phase 1: Core Infrastructure ✅
1. Create `WorldGridPresentationConfigSO.cs` - SO for all settings
2. Create interfaces: `IGridVisualizer`, `IHoverVisualizer`, `IFootprintVisualizer`
3. Create `WorldGridPresentationModule.cs` with mode system
4. Register in GameScope.cs (Module + ConfigSO)
5. Basic mode switching logic

### Phase 2: Grid Renderers ✅
1. ✅ Created `DecalGridVisualizer` (URP Decal-based, stub ready for shader)
2. ✅ Created `LineRendererGridVisualizer` (fallback, fully functional)
3. ✅ Added `GridRenderingType` enum to config
4. ✅ Factory pattern in `WorldGridPresentationModule`
5. ⏳ TODO: Create URP Shader Graph for decal grid pattern
6. ⏳ TODO: Create Decal Material in Unity Editor

### Phase 3: Hover + Footprint Visualizers
1. Implement `HoverVisualizer : IHoverVisualizer`
2. Implement `FootprintVisualizer : IFootprintVisualizer`
3. Quad pooling for footprint cells
4. Pulse animation for hover
5. Placement validation integration

### Phase 4: UI Integration
1. Add toggle button to DebugPanelUI
2. Wire up events
3. Polish animations (fade in/out)
4. Save grid visibility preference (PlayerPrefs)

### Phase 5: Polish
1. Tweak colors and alpha values
2. Add subtle pulse animation on hover
3. Optimize LineRenderer count for large grids
4. (Optional) Shader-based grid rendering

## Open Questions

1. **Grid Extent** - How large should static grid be?
   - Option A: Fixed size around world origin (e.g., 100x100)
   - Option B: Dynamic based on camera view
   - **Recommendation:** Start with Option B (view-based), easier on performance

2. **Grid Persistence** - Should grid state persist between sessions?
   - Use `PlayerPrefs.GetInt("WorldGrid_Visible", 0)` to save
   - **Recommendation:** Save visibility preference

3. **Module Placement Preview** - Modules are placed inside structures, not in world grid
   - **Decision:** No grid highlight for modules (different system)
   - Show slot highlight on structure instead (future feature)

4. **Performance** - How many LineRenderers can we afford?
   - 100x100 grid = 200 LineRenderers (100 rows + 100 columns)
   - **Recommendation:** Start simple, profile, optimize if needed
   - Fallback: Shader-based grid

5. **Visual Style** - How visible should grid be?
   - **Recommendation:** Very subtle in StaticGrid mode (alpha 0.05-0.1)
   - More visible in InteractionMode (alpha 0.2-0.3)
   - Bright highlight for hover (alpha 0.4-0.6)

## Dependencies

**Existing Systems:**
- WorldGrid (spatial indexing)
- DebugModule (state management)
- DebugPanelUI (UI layer)
- StructurePlacementService (placement validation)

**New Dependencies:**
- Camera reference (for view-based culling)
- Input system (mouse position)
- Materials for grid/highlight

## Testing Plan

1. **Manual Testing:**
   - Toggle grid on/off via UI
   - Verify grid renders correctly
   - Test hover highlight with actor spawn
   - Test footprint preview with structure placement
   - Verify state transitions (Idle → Browsing → ReadyToApply)

2. **Edge Cases:**
   - Camera at different angles
   - Very large/small cellSize values
   - Structures with 1x1 footprint
   - Structures with large footprints (10x10+)
   - Multiple DebugPanel actions in sequence

## Future Enhancements

1. **Advanced Visualization:**
   - Show actor density (heat map)
   - Highlight cells with specific tags
   - Navigation cost overlay

2. **Editor Tools:**
   - Gizmos integration for level design
   - Custom grid size per scene

3. **Performance:**
   - LOD system for grid detail
   - Instanced rendering for highlights
   - Compute shader for large grids

## Notes from Unity-Code-Expert

**Код-стайл reminder:**
- Private fields: `_camelCase`
- Public properties: `camelCase`
- Methods: `PascalCase`
- Use Odin attributes where appropriate (`[ShowInInspector]`, `[FoldoutGroup]`)
- Follow RefreshLinks pattern for component caching

**VContainer DI:**
- Register as Singleton: `builder.Register<WorldGridPresentationModule>(Lifetime.Singleton).AsSelf()`
- Inject DebugModule dependency
- Use `[Inject]` attribute for dependencies

**Performance considerations:**
- Pool LineRenderers to avoid GC
- Use object pooling for highlight quads
- Dirty flag pattern for updates
- Only update hover when mouse moves

**Material handling:**
- Create materials in code (avoid asset references for simple materials)
- Use `Material.SetColor()` for runtime tweaks
- Consider URP shader graph for advanced effects

---

**Ready to discuss?** Жду твои мысли, дополнения, или "поехали делать!" 🚀
