# World Generation Pipeline

> Phased world generation system with Artist Mode for iterative control.

---

## 🎯 Implementation Status

| Component | Status | Files |
|-----------|--------|-------|
| **Noise System** | ✅ Complete | 12 files |
| **Pipeline Core** | ✅ Complete | 4 files |
| **Generation Phases** | ✅ Complete | 5 files |
| **ScriptableConfig Refactor** | ✅ Complete | 5 configs |
| **Artist Mode Window** | ✅ Complete | SOLID refactored (15+ files) |
| **Water System** | ✅ Complete | Rivers, Lakes, Shore Styles |
| **Debug Visualization** | ✅ Complete | Overlay quad + Gizmos |

---

## Water System ✅ NEW

### Overview

Unified water handling with scene sync, per-biome shore styles, and automatic terrain carving.

### WaterPlane Sync

TerrainSculptPhase automatically syncs with scene `WaterPlane` object:
- **Read**: If WaterPlane exists, reads Y position as water level
- **Write**: After generation, updates WaterPlane Y to match config
- **Create**: Settings drawer can create WaterPlane if missing

### Lake Biomes (BiomeSO)

```csharp
// Identity section
isWaterBody = true;              // Marks biome as lake/pond
waterDepth = 3f;                 // Depth at center (0.5-15m)
shoreGradient = 0.5f;            // Shore slope (0=steep, 1=gradual)
```

Lakes are carved below water level with smooth bowl profile using quintic smootherstep.

### River Shore Styles

Each biome defines how rivers look when passing through it:

```csharp
// RiverShoreStyle.cs
public enum RiverShoreStyle {
  Natural,    // Standard smootherstep (default)
  Soft,       // Beach-like, double smoothstep (meadows)
  Rocky,      // Sharp cliffs + noise irregularity (hills, mountains)
  Marshy,     // Very gradual, extended wet zone (swamps)
  Terraced    // Step-like geological profile (man-made, canyons)
}

// BiomeSO fields (River Shore foldout)
riverShoreStyle;                 // Shore type enum
riverShoreGradient;              // Slope steepness 0-1
riverShoreWidth;                 // Transition zone (1-15m)
rockyIrregularity;               // Noise for rocky edges (0-1, Rocky only)
```

### Recommended Shore Settings

| Biome | Style | Gradient | Width | Notes |
|-------|-------|----------|-------|-------|
| Forest | Natural | 0.5 | 4m | Standard |
| Meadow | Soft | 0.8 | 6m | Sandy beaches |
| Hills | Rocky | 0.3 | 2m | Cliffs, irregularity=0.6 |
| Desert | Natural | 0.4 | 3m | Sandy |
| Swamp | Marshy | 0.9 | 10m | Boggy wetlands |

### River Carving Algorithm

1. Find biome borders using `BiomeMap.GetDistanceToBorder()`
2. Apply noise mask for river presence (`riverBorderChance`)
3. Calculate profile based on biome's `RiverShoreStyle`
4. Blend with terrain using smootherstep
5. Ensure river bed is below water level

---

## Artist Mode Window ✅ SOLID Refactored

### Architecture

Refactored from 27KB monolith to SOLID structure:

```
Editor/WorldGenerationWizard/ArtistMode/
├── ArtistModeWindow.cs           # 4.8KB - minimal shell
├── ArtistModeState.cs            # Pipeline state management
├── ArtistModeStyles.cs           # Cached GUIStyles
├── Drawers/
│   ├── HeaderDrawer.cs           # Seed, terrain, config
│   ├── PhaseListDrawer.cs        # Phase toggles + Run To
│   ├── ActionsDrawer.cs          # Run All, Reset, Quick
│   └── DebugDrawer.cs            # Debug visualization toggle
└── PhaseSettings/
    ├── IPhaseSettingsDrawer.cs   # Interface
    ├── BiomeLayoutSettingsDrawer.cs
    ├── TerrainSculptSettingsDrawer.cs  # Water settings UI
    ├── SplatmapPaintSettingsDrawer.cs
    ├── VegetationSettingsDrawer.cs
    └── ScatterSettingsDrawer.cs
```

### Key Features

- **Run To Selected**: Always resets and runs fresh to target phase
- **Phase Settings**: Context-sensitive UI per phase
- **Debug Overlay**: Single quad system (never modifies terrain material)
- **Water Sync**: Button to sync with scene WaterPlane
- **Lake Counter**: Shows how many water body biomes configured

---

## Generation Phases

### Phase 1: BiomeLayoutPhase
- Generates Voronoi diagram with domain warping
- Assigns biomes to cells based on weights
- Output: `BiomeMap`, debug material (biome colors)

### Phase 2: TerrainSculptPhase ✅ Enhanced
- **Pass 1**: Base heights + biome heights + global noise + lakes
- **Pass 2**: River carving along biome borders (style-aware)
- **Pass 3**: Water edge smoothing (3 passes)
- **Pass 4**: Slope limiting for NavMesh compatibility
- Syncs with scene WaterPlane
- Output: Terrain heightmap, `RiverMask`

### Phase 3: SplatmapPaintPhase
- Paints base texture per biome
- Applies slope/cliff textures
- Output: Terrain splatmap

### Phase 4: VegetationPhase
- Applies terrain detail layers (grass)
- Uses mask system for distribution
- Output: Detail layers

### Phase 5: ScatterPhase
- Spawns prefabs (trees, rocks)
- Uses `biome.scatterConfigs`
- Output: GameObjects under `[Generated_Scatters]`

---

## TerrainSculptPhase Config (WorldGeneratorConfig)

```csharp
// Global Noise
bool useGlobalNoise = true;
float globalNoiseAmplitude = 10f;    // Large hills (0-30m)
float globalNoiseScale = 0.008f;
float detailNoiseAmplitude = 2f;     // Fine detail (0-10m)
float detailNoiseScale = 0.05f;

// Slope Control
bool limitSlopes = true;
float maxSlopeAngle = 40f;           // NavMesh default = 45°
int slopeSmoothingPasses = 2;

// Rivers
bool generateRivers = false;
float riverWidth = 6f;               // 2-20m
float riverBorderChance = 0.3f;      // 0-1
float riverBedDepth = 1f;            // Below water (0.5-5m)

// Water
float waterLevel = 5f;               // Syncs with WaterPlane
```

---

## Debug Visualization

### Overlay Quad System
- Single quad positioned above terrain
- Shader shows biome colors (Phase 1)
- Auto-hides when no debug material returned
- Never modifies terrain material directly

### River Gizmo (RiverGizmoDrawer.cs)
- Blue water level plane
- Blue discs at river locations
- Activated after Phase 2 completion

---

## Noise System

### Available Noise Types

| Type | SO Class | Use Case |
|------|----------|----------|
| **Perlin** | `PerlinNoiseSO` | Base terrain, gentle hills |
| **Simplex** | `SimplexNoiseSO` | General purpose, less artifacts |
| **Cellular** | `CellularNoiseSO` | Biome boundaries, cracks, cells |
| **Value** | `ValueNoiseSO` | Blocky retro terrain |
| **Ridged** | `RidgedNoiseSO` | Mountains, ridges, sharp peaks |
| **Billow** | `BillowNoiseSO` | Rolling hills, puffy clouds |

### Modifiers

| Modifier | Description |
|----------|-------------|
| **FBM** | Fractal Brownian Motion - stacks octaves for detail |
| **Turbulence** | Domain warping - distorts coordinates |
| **Terrace** | Stepped output - creates plateaus |

---

## File Structure

```
World/
├── Biomes/
│   ├── BiomeSO.cs                  # Water body + river shore settings
│   ├── BiomeType.cs
│   ├── BiomeMap.cs                 # GetDistanceToBorder()
│   └── RiverShoreStyle.cs          # NEW: Shore style enum
│
├── Generation/
│   ├── Pipeline/
│   │   ├── GenerationContext.cs    # RiverMask property
│   │   ├── GenerationPipeline.cs
│   │   └── Phases/
│   │       ├── BiomeLayoutPhase.cs
│   │       ├── TerrainSculptPhase.cs  # Water sync, lakes, rivers
│   │       ├── SplatmapPaintPhase.cs
│   │       ├── VegetationPhase.cs
│   │       └── ScatterPhase.cs
│   └── Noise/
│       └── ... (12 files)
│
├── RiverGizmoDrawer.cs             # NEW: Scene gizmo visualization
└── WorldGeneratorConfigSO.cs       # Water/river/slope settings

Editor/WorldGenerationWizard/ArtistMode/
├── ArtistModeWindow.cs
├── ArtistModeState.cs
├── ArtistModeStyles.cs
├── Drawers/ (4 files)
└── PhaseSettings/ (6 files)
```

---

## ScriptableConfig Pattern

Base class for SO wrapping configuration data:

```csharp
public abstract class ScriptableConfig<TData> : SerializedScriptableObject 
  where TData : class, new() {
  
  [HideLabel, InlineProperty]
  protected TData _data = new();
  
  public TData Data => _data;
}
```

**Refactored Configs**: TreeFallConfigSO, WorldGridPresentationConfigSO, ColonyProgressionConfigSO, WorldGeneratorConfigSO, BuildingManagerConfigSO

---

## Next Steps

1. ⏳ Test river styles with different biomes
2. ⏳ Add water plane material (transparency, caustics)
3. ⏳ NavMesh baking test with slope limits
4. ⏳ Preset system for generation configs (REMINDER)
