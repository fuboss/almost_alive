# WFC для генерации структур — Концепт-документ v2

> **Статус:** 🟡 Research / Design Phase  
> **Автор:** AI Assistant  
> **Дата:** 2025-01-26  
> **Обновлено:** После обсуждения требований

---

## Оглавление

1. [Резюме требований](#резюме-требований)
2. [Что такое WFC](#что-такое-wfc)
3. [Архитектура решения](#архитектура-решения)
4. [Tile System — Модульные элементы](#tile-system--модульные-элементы)
5. [WFC Solver](#wfc-solver)
6. [Интеграция с существующей системой](#интеграция-с-существующей-системой)
7. [Asset Pipeline](#asset-pipeline)
8. [Editor Tools](#editor-tools)
9. [Runtime Flow](#runtime-flow)
10. [Оптимизация](#оптимизация)
11. [План внедрения](#план-внедрения)
12. [Технические детали](#технические-детали)
13. [Анализ существующих решений](#анализ-существующих-решений) ⭐

---

## Резюме требований

| Требование | Решение |
|------------|---------|
| **Нерегулярные формы по World Grid** | WFC генерирует footprint mask (набор GroundCoord) |
| **Стены, крыша, пол** | Модульные тайлы с socket system |
| **Подъём над террейном** | Сохраняется текущий подход + supports |
| **Модульный 3D, стены 3м** | Тайлы 1x3m (1 cell × WallHeight) |
| **Деревянные/каменные** | MaterialSet system — один тайл, разные материалы |
| **Несколько этажей** | Floor layers в WFC (Level 0, 1, 2...) |
| **NavMesh внутри** | NavMeshSurface per floor, перестройка при expansion |
| **Expansion** | WFC пересчитывает при добавлении структуры |
| **Декор** | Декоративные тайлы с visibility rules |
| **Экономия ресурсов** | GPU Instancing, shared meshes, LOD groups |
| **Editor tooling** | Визуальный редактор тайлов, preview генерации |

---

## Что такое WFC

**Wave Function Collapse (WFC)** — constraint-based процедурная генерация:

```
1. Grid ячеек в "суперпозиции" (все тайлы возможны)
2. Выбор ячейки с минимальной энтропией
3. Collapse — выбор конкретного тайла (взвешенно-рандомно)
4. Propagate — удаление несовместимых вариантов у соседей
5. Repeat до заполнения или contradiction
6. Backtrack при contradiction
```

### Почему WFC для зданий

| Альтернатива | Проблема |
|--------------|----------|
| Ручное создание | Не масштабируется, однообразие |
| Полный рандом | Сломанные соединения |
| BSP / Maze | Только прямоугольные комнаты |
| L-systems | Плохо для закрытых структур |
| **WFC** | ✅ Гарантия корректных соединений + разнообразие |

---

## Архитектура решения

### Три уровня WFC

```
┌─────────────────────────────────────────────────────────────┐
│ Level 1: FOOTPRINT                                          │
│ Генерирует: HashSet<GroundCoord> — занятые клетки           │
│ Тайлы: Floor, Empty                                         │
│ Constraints: connectivity, min/max area, aspect ratio       │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ Level 2: SHELL (Walls + Roof + Floor)                       │
│ Генерирует: Wall тайлы по периметру footprint               │
│ Тайлы: WallSolid, WallDoorway, WallWindow, WallCorner,      │
│        RoofFlat, RoofSloped, FloorWood, FloorStone          │
│ Constraints: min entries, no adjacent doorways              │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ Level 3: INTERIOR (optional)                                │
│ Генерирует: внутренние перегородки, комнаты                 │
│ Тайлы: Partition, Arch, Room markers                        │
│ Constraints: room connectivity, min room size               │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ Level 4: DECORATION                                         │
│ Генерирует: колонны, балки, накладки, детали                │
│ Тайлы: Column, Beam, Trim, Props                            │
│ Constraints: support columns at corners, beam spans         │
└─────────────────────────────────────────────────────────────┘
```

### Ключевые классы

```csharp
// ========== DATA ==========

/// Набор тайлов для генерации
public class WFCTileSetSO : ScriptableObject {
    public WFCTile[] tiles;
    public SocketDefinition[] sockets;
    public MaterialSetSO[] materialSets;  // wood, stone, etc.
}

/// Один тайл
[Serializable]
public class WFCTile {
    public string id;
    public GameObject prefab;
    public TileCategory category;         // Wall, Floor, Roof, Decor
    public SocketType north, south, east, west, up, down;
    public float weight = 1f;             // probability
    public bool canRotate;
    public bool canMirror;
    public int[] validFloors;             // which floor levels allowed
}

/// Тип соединения
public enum SocketType {
    Empty,          // nothing can connect
    WallBase,       // standard wall bottom
    WallTop,        // standard wall top  
    FloorEdge,      // floor connects here
    RoofEdge,       // roof connects here
    DoorwayBase,    // doorway bottom (compatible with WallBase neighbors)
    WindowMid,      // window middle section
    // ... extensible
}

/// Результат WFC генерации
public class WFCStructureLayout {
    public HashSet<GroundCoord> footprint;
    public Dictionary<GroundCoord, WFCCell> cells;
    public int floors;
    public int seed;
    
    public class WFCCell {
        public WFCTile wallTile;      // null if interior
        public WFCTile floorTile;
        public WFCTile roofTile;      // only top floor
        public WFCTile[] decorTiles;
        public int rotation;          // 0, 90, 180, 270
    }
}

// ========== SOLVER ==========

public interface IWFCSolver {
    WFCStructureLayout Solve(WFCGenerationRequest request);
    bool Validate(WFCStructureLayout layout);
}

public class WFCGenerationRequest {
    public WFCTileSetSO tileSet;
    public int targetArea;            // approximate cells
    public int floors = 1;
    public int minEntries = 1;
    public int maxEntries = 4;
    public GroundCoord? anchorCell;   // must include this cell
    public WallSide? entryDirection;  // preferred entry side
    public int? seed;                 // for reproducibility
    public MaterialSetSO materialSet;
}

// ========== RUNTIME ==========

public class WFCStructureBuilder {
    public Structure Build(WFCStructureLayout layout, Vector3 worldPosition, Terrain terrain);
    public void Rebuild(Structure structure, WFCStructureLayout newLayout);
    public void Expand(Structure source, Structure expansion, StructureConnection connection);
}
```

---

## Tile System — Модульные элементы

### Размеры и Grid

```
World Grid:
- cellSize = 1m (from WorldGrid.cellSize)
- WallHeight = 3m (from BuildingConstants.WallHeight)

Tile Dimensions:
- Floor tile: 1m × 1m × 0.1m (covers one GroundCoord)
- Wall tile:  1m × 3m × 0.2m (one cell width, full height)
- Roof tile:  1m × 1m × 0.5m (covers one GroundCoord)
- Corner:     0.2m × 3m × 0.2m (pillar at corners)
```

### Минимальный TileSet (MVP)

```
WALLS (8 tiles):
├── wall_solid           — глухая стена
├── wall_doorway         — стена с проёмом двери
├── wall_window          — стена с окном
├── wall_passage         — арка/проход для expansion
├── wall_corner_outer    — внешний угол (L-образный)
├── wall_corner_inner    — внутренний угол
├── wall_half_left       — половина стены (для T-junction)
└── wall_half_right

FLOORS (4 tiles):
├── floor_wood_full      — полный деревянный пол
├── floor_stone_full     — каменный пол
├── floor_wood_edge      — край платформы
└── floor_hatch          — люк для лестницы (multi-floor)

ROOFS (6 tiles):
├── roof_flat            — плоская крыша
├── roof_sloped_end      — скат (торец)
├── roof_sloped_mid      — скат (середина)
├── roof_ridge           — конёк
├── roof_corner          — угол крыши
└── roof_overhang        — свес крыши

DECOR (10+ tiles):
├── column_wood          — деревянная колонна
├── column_stone         — каменная колонна
├── beam_wood            — потолочная балка
├── trim_base            — плинтус
├── trim_top             — карниз
├── shutter_left         — ставень левый
├── shutter_right        — ставень правый
├── torch_holder         — крепление для факела
├── planter              — ящик для цветов
└── sign_blank           — вывеска
```

### Socket Compatibility Matrix

```
           │ Empty │WallBase│WallTop│FloorEdge│DoorwayBase│
───────────┼───────┼────────┼───────┼─────────┼───────────│
Empty      │   ✓   │   ✗    │   ✗   │    ✗    │     ✗     │
WallBase   │   ✗   │   ✓    │   ✗   │    ✓    │     ✓     │
WallTop    │   ✗   │   ✗    │   ✓   │    ✗    │     ✗     │
FloorEdge  │   ✗   │   ✓    │   ✗   │    ✓    │     ✓     │
DoorwayBase│   ✗   │   ✓    │   ✗   │    ✓    │     ✗     │
```

### Material Sets (экономия ассетов)

```csharp
public class MaterialSetSO : ScriptableObject {
    public string setId;  // "wood", "stone", "adobe"
    public Material wallMaterial;
    public Material floorMaterial;
    public Material roofMaterial;
    public Material trimMaterial;
}
```

Один prefab тайла → разные MaterialSets = визуальное разнообразие без дублирования mesh-ей.

---

## WFC Solver

### Алгоритм (Simple Model)

```csharp
public class WFCSimpleSolver : IWFCSolver {
    
    public WFCStructureLayout Solve(WFCGenerationRequest request) {
        var random = request.seed.HasValue 
            ? new System.Random(request.seed.Value) 
            : new System.Random();
        
        // Phase 1: Generate Footprint
        var footprint = GenerateFootprint(request, random);
        if (footprint == null) return null;
        
        // Phase 2: Initialize grid
        var grid = InitializeGrid(footprint, request);
        
        // Phase 3: WFC loop
        while (!IsFullyCollapsed(grid)) {
            var cell = SelectLowestEntropyCell(grid, random);
            if (cell == null) {
                // Contradiction - backtrack or fail
                if (!Backtrack(grid)) return null;
                continue;
            }
            
            Collapse(cell, random);
            Propagate(grid, cell);
        }
        
        // Phase 4: Build result
        return BuildLayout(grid, footprint, request.seed ?? 0);
    }
    
    private HashSet<GroundCoord> GenerateFootprint(
        WFCGenerationRequest request, 
        System.Random random
    ) {
        // Start from anchor or random cell
        var start = request.anchorCell ?? new GroundCoord(0, 0);
        var cells = new HashSet<GroundCoord> { start };
        
        // Grow organically until target area
        var frontier = new List<GroundCoord> { start };
        
        while (cells.Count < request.targetArea && frontier.Count > 0) {
            var idx = random.Next(frontier.Count);
            var current = frontier[idx];
            
            var neighbors = GetNeighbors(current);
            random.Shuffle(neighbors);
            
            foreach (var neighbor in neighbors) {
                if (!cells.Contains(neighbor) && ShouldExpand(cells, neighbor, request)) {
                    cells.Add(neighbor);
                    frontier.Add(neighbor);
                    if (cells.Count >= request.targetArea) break;
                }
            }
            
            // Remove cells with no expansion potential
            if (!HasExpansionPotential(current, cells)) {
                frontier.RemoveAt(idx);
            }
        }
        
        return cells;
    }
}
```

### Entropy & Propagation

```csharp
private void Propagate(WFCGrid grid, WFCGridCell collapsedCell) {
    var stack = new Stack<WFCGridCell>();
    stack.Push(collapsedCell);
    
    while (stack.Count > 0) {
        var cell = stack.Pop();
        var tile = cell.collapsedTile;
        
        foreach (var dir in Directions.All) {
            var neighbor = grid.GetNeighbor(cell.coord, dir);
            if (neighbor == null || neighbor.IsCollapsed) continue;
            
            var requiredSocket = tile.GetSocket(dir);
            var oppositeDir = dir.Opposite();
            
            var before = neighbor.possibleTiles.Count;
            neighbor.possibleTiles.RemoveAll(t => 
                !IsSocketCompatible(t.GetSocket(oppositeDir), requiredSocket)
            );
            
            if (neighbor.possibleTiles.Count < before) {
                stack.Push(neighbor);
            }
            
            if (neighbor.possibleTiles.Count == 0) {
                throw new WFCContradictionException(neighbor.coord);
            }
        }
    }
}
```

---

## Интеграция с существующей системой

### Изменения в StructureDefinitionSO

```csharp
public class StructureDefinitionSO : SerializedScriptableObject {
    // ...existing code...
    
    [Title("Generation Mode")]
    [EnumToggleButtons]
    public StructureGenerationMode generationMode = StructureGenerationMode.Fixed;
    
    // Fixed mode (current)
    [ShowIf("generationMode", StructureGenerationMode.Fixed)]
    public Vector2Int footprint = new(3, 3);
    
    [ShowIf("generationMode", StructureGenerationMode.Fixed)]
    public GameObject foundationPrefab;
    
    // WFC mode (new)
    [ShowIf("generationMode", StructureGenerationMode.WFC)]
    public WFCTileSetSO tileSet;
    
    [ShowIf("generationMode", StructureGenerationMode.WFC)]
    public WFCGenerationPreset generationPreset;
    
    [ShowIf("generationMode", StructureGenerationMode.WFC)]
    [Range(4, 100)]
    public int targetArea = 9;
    
    [ShowIf("generationMode", StructureGenerationMode.WFC)]
    [Range(1, 3)]
    public int maxFloors = 1;
}

public enum StructureGenerationMode {
    Fixed,  // current behavior
    WFC     // procedural
}

[Serializable]
public class WFCGenerationPreset {
    public int minEntries = 1;
    public int maxEntries = 2;
    public float doorwayChance = 0.3f;
    public float windowChance = 0.4f;
    public bool allowInnerCorners = true;
    public float decorDensity = 0.5f;
}
```

### Изменения в StructureConstructionService

```csharp
public class StructureConstructionService {
    [Inject] private IWFCSolver _wfcSolver;
    [Inject] private WFCStructureBuilder _wfcBuilder;
    
    public void BuildStructure(Structure structure, Terrain terrain) {
        var definition = structure.definition;
        
        if (definition.generationMode == StructureGenerationMode.WFC) {
            BuildWFCStructure(structure, terrain);
        } else {
            BuildFixedStructure(structure, terrain);  // existing logic
        }
    }
    
    private void BuildWFCStructure(Structure structure, Terrain terrain) {
        var request = new WFCGenerationRequest {
            tileSet = structure.definition.tileSet,
            targetArea = structure.definition.targetArea,
            floors = structure.definition.maxFloors,
            // ... from preset
        };
        
        var layout = _wfcSolver.Solve(request);
        if (layout == null) {
            Debug.LogError("[WFC] Generation failed, falling back to simple box");
            // Fallback to 3x3 box
            layout = GenerateFallbackLayout(request);
        }
        
        structure.wfcLayout = layout;
        _wfcBuilder.Build(layout, structure.transform.position, terrain);
        
        // Generate slots from layout
        GenerateSlotsFromLayout(structure, layout);
        
        _navigationModule.RegisterSurface(structure.navMeshSurface);
        structure.SetState(StructureState.BUILT);
    }
}
```

### Expansion с WFC

```csharp
public void ExpandStructure(Structure source, Structure expansion, SnapPoint snapPoint) {
    // 1. Get combined footprint
    var combinedFootprint = new HashSet<GroundCoord>(source.wfcLayout.footprint);
    
    // 2. Calculate expansion anchor
    var connectionCell = GetConnectionCell(source, snapPoint);
    
    // 3. Generate expansion layout constrained to connect
    var expansionRequest = new WFCGenerationRequest {
        tileSet = expansion.definition.tileSet,
        targetArea = expansion.definition.targetArea,
        anchorCell = connectionCell,
        existingFootprint = combinedFootprint,  // avoid overlap
        requiredConnection = snapPoint.side.Opposite()
    };
    
    var expansionLayout = _wfcSolver.Solve(expansionRequest);
    
    // 4. Update source wall at connection point → Passage
    UpdateWallToPassage(source, snapPoint);
    
    // 5. Build expansion
    _wfcBuilder.Build(expansionLayout, ...);
    
    // 6. Create connection
    var connection = new StructureConnection(source, expansion, ...);
    source.connectionsInternal.Add(connection);
    expansion.connectionsInternal.Add(connection);
    
    // 7. Rebuild NavMesh for both
    _navigationModule.RebuildArea(GetCombinedBounds(source, expansion));
}
```

---

## Asset Pipeline

### Требования к Prefab тайла

```
Prefab Structure:
├── Root (with WFCTileMarker component)
│   ├── Mesh (LOD Group recommended)
│   │   ├── LOD0
│   │   ├── LOD1
│   │   └── LOD2 (optional)
│   ├── Collider (simplified)
│   └── [Optional] SnapPoints (for decor attachment)

WFCTileMarker component:
- tileId: string
- category: TileCategory
- sockets: SocketType[6] (±X, ±Y, ±Z)
- pivot: TilePivot (Center, Corner, Edge)
- size: Vector3Int (usually 1,3,1 for walls)
- canRotate: bool
- canMirror: bool
- materialSlots: string[] (for MaterialSet override)
```

### Naming Convention

```
{category}_{type}_{variant}_{material}

Examples:
- wall_solid_01_wood
- wall_doorway_arched_stone
- floor_plank_worn_wood
- roof_sloped_end_thatch
- decor_column_carved_stone
```

### LOD Requirements

```
LOD0: Full detail     (0-15m)   — для close-up
LOD1: Simplified      (15-30m)  — меньше полигонов
LOD2: Billboard/Box   (30m+)    — distant view

Для декора:
LOD0: Full mesh
LOD1: Sprite impostor
```

### Addressables Groups

```
StructureTiles/
├── TileSets/
│   ├── TileSet_Wooden.asset
│   ├── TileSet_Stone.asset
│   └── TileSet_Mixed.asset
├── Tiles_Walls/
│   ├── wall_solid_01.prefab
│   └── ...
├── Tiles_Floors/
├── Tiles_Roofs/
├── Tiles_Decor/
└── MaterialSets/
    ├── MaterialSet_Wood.asset
    ├── MaterialSet_Stone.asset
    └── MaterialSet_Adobe.asset
```

---

## Editor Tools

### WFC Tile Editor Window

```csharp
public class WFCTileEditorWindow : OdinEditorWindow {
    [MenuItem("Tools/WFC/Tile Editor")]
    public static void Open() => GetWindow<WFCTileEditorWindow>();
    
    // Features:
    // - Socket visual editor (colored cubes on faces)
    // - Tile preview with rotation
    // - Compatibility matrix visualization
    // - Batch socket assignment
    // - Validation (orphan tiles, missing sockets)
}
```

### WFC Preview Tool

```csharp
public class WFCPreviewTool : EditorWindow {
    [MenuItem("Tools/WFC/Preview Generator")]
    public static void Open() => GetWindow<WFCPreviewTool>();
    
    // Features:
    // - Seed input
    // - Target area slider
    // - Generate button → shows in Scene view
    // - Regenerate with same/new seed
    // - Save as StructureLayoutSO
    // - Export stats (tile distribution)
}
```

### Scene Gizmos

```csharp
[CustomEditor(typeof(Structure))]
public class StructureWFCGizmos : Editor {
    void OnSceneGUI() {
        var structure = (Structure)target;
        if (structure.wfcLayout == null) return;
        
        // Draw footprint cells
        foreach (var coord in structure.wfcLayout.footprint) {
            var worldPos = coord.ToWorld() + structure.transform.position;
            Handles.DrawWireCube(worldPos, Vector3.one * WorldGrid.cellSize);
        }
        
        // Draw socket connections (debug mode)
        if (showSocketDebug) {
            DrawSocketConnections(structure.wfcLayout);
        }
    }
}
```

---

## Runtime Flow

### Placement Flow (Player)

```
1. Player opens Build Menu
2. Selects WFC structure type (e.g., "Wooden Shelter")
3. Ghost preview shows approximate footprint (semi-transparent)
   - При движении мыши генерируется новый layout (throttled)
   - Или: показывается "area outline" без деталей
4. Player clicks → confirms position
5. WFC generates final layout
6. UnfinishedStructureActor created with layout reference
7. Agents deliver resources & build
8. On complete: WFCBuilder instantiates tiles
```

### Generation Timing

```
Option A: Generate on placement confirmation
- Pro: Final layout when player commits
- Con: Slight delay on click

Option B: Pre-generate pool of layouts
- Pro: Instant placement
- Con: Memory overhead, less customization

Option C: Background generation (async)
- Pro: No UI stutter
- Con: Complexity

Recommendation: Option A with 100ms budget, fallback to cached simple layouts
```

### Performance Budget

```
Target: <100ms for generation (main thread)
        <16ms for instantiation per frame (batched)

Breakdown:
- Footprint generation: ~10ms
- Wall WFC: ~30ms  
- Interior WFC: ~20ms
- Decor WFC: ~15ms
- Validation: ~5ms
- Buffer: ~20ms

For larger structures (>50 cells): Use async/coroutine
```

---

## Оптимизация

### GPU Instancing

```csharp
// All tiles use same material with GPU Instancing enabled
// Per-instance properties: _Color, _WearLevel, etc.

[MaterialProperty("_Color")]
public Color tintColor = Color.white;

[MaterialProperty("_WearLevel")]
public float wear = 0f;
```

### Mesh Combining (Optional)

```csharp
public class StructureMeshCombiner {
    /// Combine static tiles into single mesh after build
    public void CombineStaticMeshes(Structure structure) {
        // Group by material
        // Combine using CombineInstance
        // Replace individual renderers with combined
        // Keep dynamic tiles (doors, etc.) separate
    }
}
```

### Culling

```csharp
// Per-floor culling for multi-story
public class FloorCullingController : MonoBehaviour {
    public void SetFloorVisible(int floor, bool visible) {
        // Toggle renderer.enabled for floor group
        // Update NavMeshSurface.enabled
    }
}

// Interior culling when camera outside
public class InteriorCullingController : MonoBehaviour {
    void Update() {
        var cameraInside = IsPointInside(Camera.main.transform.position);
        SetInteriorVisible(cameraInside);
    }
}
```

### Object Pooling

```csharp
// Decor tiles are pooled
public class WFCDecorPool : MonoBehaviour {
    private Dictionary<string, Queue<GameObject>> _pools;
    
    public GameObject Get(string tileId) { ... }
    public void Return(GameObject instance) { ... }
}
```

---

## План внедрения

### Phase 1: Foundation (2 недели)

**Week 1:**
- [ ] `WFCTile`, `WFCTileSetSO`, `SocketType` data classes
- [ ] Basic `WFCSimpleSolver` (footprint only)
- [ ] Editor window: TileSet creator
- [ ] Unit tests for solver

**Week 2:**
- [ ] Wall tile socket system
- [ ] WFC for wall placement (Level 2)
- [ ] Integration with `StructureConstructionService`
- [ ] Scene gizmos for debug

**Deliverable:** WFC генерирует нерегулярные формы с корректными стенами

### Phase 2: Full Shell (2 недели)

**Week 3:**
- [ ] Floor tiles
- [ ] Roof tiles
- [ ] Multi-floor support
- [ ] MaterialSet system

**Week 4:**
- [ ] Corner handling (inner/outer)
- [ ] Doorway/Window constraints
- [ ] Entry point detection from layout
- [ ] NavMesh generation for irregular shapes

**Deliverable:** Полноценные закрытые структуры любой формы

### Phase 3: Decoration (1 неделя)

- [ ] Decor tile system
- [ ] Column/Beam rules (structural logic)
- [ ] Visibility conditions (StructureDecoration integration)
- [ ] LOD setup

**Deliverable:** Визуально богатые структуры с декором

### Phase 4: Editor Tools (1 неделя)

- [ ] Socket visual editor
- [ ] Preview generator
- [ ] Tile validation
- [ ] Batch operations

**Deliverable:** Удобный workflow для artists

### Phase 5: Expansion & Polish (2 недели)

- [ ] WFC-aware expansion
- [ ] NavMesh rebuild on expand
- [ ] Performance optimization
- [ ] Fallback layouts
- [ ] Save/Load layout support

**Deliverable:** Production-ready система

---

## Технические детали

### Dependency Injection

```csharp
// In your VContainer scope:
builder.Register<IWFCSolver, WFCSimpleSolver>(Lifetime.Singleton);
builder.Register<WFCStructureBuilder>(Lifetime.Singleton);
builder.Register<WFCDecorPool>(Lifetime.Singleton);
```

### Serialization

```csharp
// WFCStructureLayout saved with Structure for save/load
[Serializable]
public class WFCStructureLayoutData {
    public int[] footprintX;
    public int[] footprintZ;
    public WFCCellData[] cells;
    public int seed;
    
    public static WFCStructureLayoutData From(WFCStructureLayout layout) { ... }
    public WFCStructureLayout ToLayout(WFCTileSetSO tileSet) { ... }
}
```

### Error Handling

```csharp
public class WFCContradictionException : Exception {
    public GroundCoord Cell { get; }
    public WFCContradictionException(GroundCoord cell) 
        : base($"WFC contradiction at ({cell.x}, {cell.z})") {
        Cell = cell;
    }
}

// In solver:
try {
    return SolveInternal(request);
} catch (WFCContradictionException e) {
    Debug.LogWarning($"[WFC] Contradiction at {e.Cell}, retrying with new seed");
    request.seed = random.Next();
    return Solve(request);  // retry once
}
```

---

## Открытые вопросы (для дальнейшего обсуждения)

1. ~~**Стартовать с Tessera или кастомный solver?**~~ → ✅ **Решено: selfsame WFC (MIT, бесплатно)**

2. **Сколько тайлов создать для MVP?**
   - Minimum: 15-20
   - Comfortable: 30-40
   - Rich: 60+

3. **Как handling multi-floor stairs?**
   - Фиксированные позиции лестниц?
   - WFC решает где лестницы?

4. **Декор: WFC или scatter?**
   - WFC для структурных элементов (колонны, балки)
   - Scatter для мелочи (горшки, таблички)

5. **Формат конфига тайлов?**
   - selfsame использует XML
   - Мы хотим ScriptableObject (WFCTileSetSO)
   - Нужен конвертер или полная замена

---

## Анализ существующих решений

### selfsame/unity-wave-function-collapse ⭐ РЕКОМЕНДУЕТСЯ

**Источник:** [selfsame.itch.io/unitywfc](https://selfsame.itch.io/unitywfc)  
**GitHub:** [selfsame/unity-wave-function-collapse](https://github.com/selfsame/unity-wave-function-collapse)  
**Лицензия:** MIT (бесплатно, можно модифицировать)

#### Что это

Полноценная Unity-реализация WFC от Sylvan (marian42). Включает:
- **Simple Tiled Model** — наш основной use case (дискретные тайлы с сокетами)
- **Overlapping Model** — генерация на основе примера (текстуры, паттерны)
- **3D поддержка** из коробки
- **Editor tools** для настройки тайлов
- **Runtime generation** с seed

#### Ключевые компоненты

```
unity-wave-function-collapse/
├── SimpleTiledModel/
│   ├── SimpleTiledModel.cs      — основной solver
│   ├── SimpleTiledModelTile.cs  — компонент для prefab'ов
│   └── TileConfig.xml           — конфигурация соединений
├── OverlappingModel/
│   └── ...                      — для текстур/паттернов
├── Editor/
│   └── WFCEditor.cs             — инспектор для настройки
└── Examples/
    └── 3D Tiles/                — примеры 3D тайлов
```

#### Как работает (Simple Tiled Model)

```csharp
// 1. Каждый тайл-prefab имеет компонент SimpleTiledModelTile
public class SimpleTiledModelTile : MonoBehaviour {
    public string tileName;
    public float weight = 1f;
    // Сокеты определяются в XML или через редактор
}

// 2. XML конфигурация соединений (можно заменить на SO)
<set>
  <tiles>
    <tile name="wall_solid" symmetry="X"/>
    <tile name="wall_corner" symmetry="L"/>
    <tile name="floor" symmetry="X"/>
  </tiles>
  <neighbors>
    <neighbor left="wall_solid" right="wall_solid"/>
    <neighbor left="wall_solid" right="wall_corner"/>
    <neighbor left="floor" right="floor"/>
  </neighbors>
</set>

// 3. Генерация
var model = new SimpleTiledModel(config, width, height, depth, periodic, seed);
bool success = model.Run(limit: 1000);  // iterations limit
if (success) {
    var result = model.GetResult();  // 3D array of tile indices
}
```

#### Плюсы для нашего проекта

| Плюс | Описание |
|------|----------|
| ✅ **MIT лицензия** | Бесплатно, можно модифицировать |
| ✅ **3D из коробки** | Не нужно адаптировать 2D код |
| ✅ **Проверенный алгоритм** | Основан на оригинале mxgmn |
| ✅ **Unity-native** | MonoBehaviour, Editor integration |
| ✅ **Простая интеграция** | Можно скопировать нужные файлы |
| ✅ **Примеры** | Есть готовые 3D tile examples |
| ✅ **Backtracking** | Есть обработка contradictions |

#### Минусы / Что нужно доработать

| Минус | Решение |
|-------|---------|
| ⚠️ XML конфиг | Заменить на ScriptableObject (наш WFCTileSetSO) |
| ⚠️ Нет async/Jobs | Обернуть в UniTask / добавить Job-версию |
| ⚠️ Старый код (2017) | Рефакторинг под современный C# |
| ⚠️ Нет MaterialSet | Добавить нашу систему материалов |
| ⚠️ Нет integration | Написать адаптер под StructureConstructionService |

#### План интеграции selfsame WFC

```
Phase 0: Импорт и адаптация (3-5 дней)
├── Скачать репозиторий
├── Извлечь SimpleTiledModel (ядро алгоритма)
├── Рефакторинг:
│   ├── XML → WFCTileSetSO
│   ├── Добавить namespace Content.Scripts.Building.WFC
│   └── Убрать лишние зависимости
├── Обернуть в IWFCSolver interface
└── Добавить UniTask async wrapper

Phase 1: Интеграция с Building System
├── WFCStructureAdapter — конвертирует результат в WFCStructureLayout
├── Связать с StructureConstructionService
└── Editor preview tool
```

#### Пример адаптера

```csharp
public class SelfsameWFCSolverAdapter : IWFCSolver {
    public WFCStructureLayout Solve(WFCGenerationRequest request) {
        // Конвертируем наш request в формат selfsame
        var config = ConvertTileSetToConfig(request.tileSet);
        var model = new SimpleTiledModel(
            config,
            request.targetArea,  // approximate width
            1,                   // height (floors handled separately)
            request.targetArea,  // depth
            periodic: false,
            seed: request.seed ?? Environment.TickCount
        );
        
        // Запускаем генерацию
        bool success = model.Run(limit: 5000);
        if (!success) return null;
        
        // Конвертируем результат в наш формат
        return ConvertResultToLayout(model.GetResult(), request);
    }
    
    public async UniTask<WFCStructureLayout> SolveAsync(
        WFCGenerationRequest request, 
        CancellationToken token = default
    ) {
        return await UniTask.RunOnThreadPool(() => Solve(request), cancellationToken: token);
    }
}
```

#### Сравнение с альтернативами

| Критерий | selfsame | Tessera | Custom |
|----------|----------|---------|--------|
| **Цена** | Бесплатно | $50 | Бесплатно |
| **3D** | ✅ | ✅ | Писать |
| **Лицензия** | MIT | Asset Store | Своя |
| **Код доступ** | ✅ Полный | ❌ Obfuscated | ✅ Полный |
| **Документация** | Минимальная | Хорошая | Своя |
| **Поддержка** | Нет | Есть | Своя |
| **Кастомизация** | ✅ Легко | ⚠️ Ограничена | ✅ Полная |
| **Время старта** | ~1 неделя | ~2 дня | ~3 недели |

#### Рекомендация

**Использовать selfsame как базу:**
1. Берём ядро алгоритма (`SimpleTiledModel.cs`)
2. Адаптируем под наши data structures
3. Добавляем async/Jobs wrapper
4. Интегрируем с существующей Building System

Это даёт нам:
- ⏱️ Экономия 2-3 недели на написание solver с нуля
- 🔧 Полный контроль над кодом (MIT)
- 🎯 Проверенный алгоритм
- 💰 $0 вместо $50 (Tessera)

---

## Ссылки

- [selfsame/unity-wave-function-collapse](https://github.com/selfsame/unity-wave-function-collapse) ⭐ **Рекомендуется**
- [mxgmn/WaveFunctionCollapse](https://github.com/mxgmn/WaveFunctionCollapse) — оригинальный алгоритм
- [Tessera Unity Asset](https://assetstore.unity.com/packages/tools/level-design/tessera-158185) — платная альтернатива
- [WFC in 3D Buildings (talk)](https://www.youtube.com/watch?v=0bcZb-SsnrA)
- [Oskar Stålberg - Townscaper](https://www.youtube.com/watch?v=1hqt8JkYRdI)

---

*Документ будет обновляться по мере разработки.*

---

## Execution model: Jobs vs UniTask — выбор механизма исполнения

Генерация WFC может иметь разные потребности по времени выполнения и по характеру работы (CPU-bound vs IO/awaitable). Рекомендуется поддержать оба режима и выбирать их в зависимости от размера задачи и наличия Burst/Jobs в проекте.

Критерии выбора:
- Unity Jobs + Burst
  - Подходит для тяжёлых, параллелизируемых участков: расчёт возможных вариантов, массовая проверка сокетов, propagation по большой сетке (>50-100 ячеек).
  - Требует перевод данных в NativeArray / простые POD-структуры. Отлично сочетается с Burst для максимальной скорости.
  - Минус: сложнее отлаживать в Editor, не позволяет прямой работы с UnityEngine.Object внутри job'ов (инстанциацию делать в main thread).

- UniTask / async (или Coroutine)
  - Подходит для задач, где нужен асинхронный поток управления, тайм-слойсинг (yield), интеграция с UI (preview) и отмена через CancellationToken.
  - Удобен для небольших/средних структур и когда требуется не блокировать основной поток. Легко использовать progress reporting и интегрируется с Editor через UniTask.Editor.
  - Минус: без параллелизации медленнее на больших объёмах по сравнению с Jobs+Burst.

Рекомендации по применению:
- Малые структуры (<30 ячеек): UniTask async generation (быстрая разработка, простая отмена и preview).
- Средние структуры (30-80 ячеек): UniTask с time-slicing (yield каждые N итераций) или комбинированный подход — критическая агрегация в Job'ах.
- Большие структуры (>80-100 ячеек): основной WFC solver на Jobs+Burst, с передачей результатов в main-thread для instantiation.

Интерфейс выбора режима (пример):

```csharp
public enum WFCExecutionMode { Auto, JobsBurst, UniTask }

public class WFCGenerationRequest {
    // ...existing fields...
    public WFCExecutionMode executionMode = WFCExecutionMode.Auto;
}

public interface IWFCSolver {
    // Возвращает layout синхронно или null если используется async путь
    WFCStructureLayout SolveSync(WFCGenerationRequest request);

    // Асинхронный вариант, выбирается если задача должна быть cancellable
    UniTask<WFCStructureLayout> SolveAsync(WFCGenerationRequest request, CancellationToken token = default);
}
```

Пример: простой адаптер внутри `WFCSimpleSolver` выбирает реализацию:

```csharp
public UniTask<WFCStructureLayout> SolveAsync(WFCGenerationRequest request, CancellationToken token = default) {
    if (request.executionMode == WFCExecutionMode.JobsBurst) {
        return UniTask.Run(() => {
            // Запускаем Jobs-based pipeline синхронно в background thread (без доступа к Unity API)
            var layout = SolveWithJobs(request);
            return layout;
        }, cancellationToken: token);
    }

    // Default: incremental UniTask implementation
    return SolveWithUniTask(request, token);
}
```

Пример шаблона Job-based стадии (псевдокод):

```csharp
// Подготовка данных
var cellCount = footprint.Count;
var possibleTiles = new NativeList<int>(allocator);
// ... fill native arrays ...

// Job: propagate constraints
var propagateJob = new PropagateJob {
    cells = cellsNative,
    possibleTiles = possibleTilesNative,
    // ...
};
var handle = propagateJob.Schedule();
handle.Complete();

// Сбор результатов в managed структуру
var layout = BuildLayoutFromNative(cellsNative);
```

Важно: все `Instantiate` / `GameObject` операции выполняются в main thread. Лучше возвращать `WFCStructureLayout` с указанием координат, тайлов и rotation, а затем в main thread вызывать `WFCStructureBuilder.Build(...)`.

Отмена и прогресс:
- UniTask: использовать CancellationToken, IProgress<float> для прогресса; удобно в Editor для preview.
- Jobs: cancellation сложнее — можно проверять флаг atomic bool между итерациями и завершать ранний выход, или разбивать на последовательные Job'ы и проверять токен между ними.

Параллельный гибридный подход:
- Использовать Jobs для "горячих" циклов propagation и подсчёта совместимых вариантов.
- Использовать UniTask для оркестрации (запуск Job'ов, ожидание их, сбор результатов и обновление UI).

Память и безопасность:
- Использовать NativeArray/NativeList и освобождать в finally блоке.
- Ограничить аллокации на каждый запуск — переиспользовать буферы для частых генераций.

Пример интеграции в `StructureConstructionService`:

```csharp
var task = _wfcSolver.SolveAsync(request, token);
// show progress in UI
var layout = await task;
if (layout == null) { fallback... }
// build on main thread
_wfcBuilder.Build(layout, position, terrain);
```
