# World Generation Pipeline

> Phased world generation system with Artist Mode for iterative control.

---

## 🎯 Implementation Status

| Component | Status | Files |
|-----------|--------|-------|
| **Noise System** | ✅ Complete | 12 files |
| **Pipeline Core** | ✅ Complete | 4 files |
| **Generation Phases** | ✅ Complete | 5 files |
| **ScriptableConfig Refactor** | ✅ Complete | 6 configs |
| **BiomeSO Decomposition** | ✅ Complete | ScriptableConfig<BiomeData> |
| **Vegetation System** | ✅ Complete | Category-based noise |
| **Artist Mode Window** | ✅ Complete | SOLID refactored (15+ files) |
| **Water System** | ✅ Complete | Rivers, Lakes, Shore Styles |
| **Debug Visualization** | ✅ Refactored | Gizmo-only (no quad) |

---

## Debug Visualization ✅ REFACTORED

### Overview

All debug visualization now uses **Scene Gizmos** instead of overlay quads. This prevents occlusion issues and integrates with Unity's Gizmo toggle system.

### Gizmo Drawers

| Drawer | Purpose | Location |
|--------|---------|----------|
| `BiomeOverlayGizmoDrawer` | Colored biome regions grid | Editor/World/ |
| `BiomeGizmoDrawer` | Cell center labels & discs | Editor/World/ |
| `RiverGizmoDrawer` | Water level plane + river markers | World/ |

### BiomeOverlayGizmoDrawer

- Draws 48x48 grid of colored quads via `Handles.DrawSolidRectangleWithOutline`
- Adapts to terrain height (follows terrain surface)
- Semi-transparent (45% alpha) to show terrain underneath
- Controlled by `debugSettings.drawBiomeGizmos`

### Removed Components

- ❌ `GenerationContext.SetDebugMaterial()` - deleted
- ❌ `GenerationContext._debugQuad` - deleted  
- ❌ `BiomeLayoutPhase.CreateDebugMaterial()` - deleted
- ❌ `IGenerationPhase.GetDebugMaterial()` - deleted
- ❌ `ArtistModeState.ShowDebugVisualization` - deleted

### Toggle

All biome gizmos controlled via single flag in `WorldGeneratorDebugSettings`:
```csharp
debugSettings.drawBiomeGizmos = true; // Enable/disable all biome visualization
```

---

## Water System ✅ 

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
│   ├── StatusDrawer.cs           # Pipeline status
│   └── DebugSettingsDrawer.cs    # NEW: Inline debug viz controls
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
- **Gizmo Visualization**: Automatic via Scene Gizmos (no manual toggle needed)
- **Water Sync**: Button to sync with scene WaterPlane
- **Lake Counter**: Shows how many water body biomes configured

---

## Generation Phases

### Phase 1: BiomeLayoutPhase
- Generates Voronoi diagram with domain warping
- Assigns biomes to cells based on weights
- Output: `BiomeMap` (cached for gizmo drawing)

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
│   └── RiverShoreStyle.cs          # Shore style enum
│
├── Generation/
│   ├── Pipeline/
│   │   ├── GenerationContext.cs    # RiverMask property (no debug quad)
│   │   ├── GenerationPipeline.cs
│   │   ├── GenerationPhaseBase.cs  # No GetDebugMaterial
│   │   ├── IGenerationPhase.cs     # No GetDebugMaterial
│   │   └── Phases/
│   │       ├── BiomeLayoutPhase.cs # No CreateDebugMaterial
│   │       ├── TerrainSculptPhase.cs
│   │       ├── SplatmapPaintPhase.cs
│   │       ├── VegetationPhase.cs
│   │       └── ScatterPhase.cs
│   └── Noise/
│       └── ... (12 files)
│
├── RiverGizmoDrawer.cs             # Scene gizmo visualization
├── WorldGeneratorConfigSO.cs       # Water/river/slope settings
└── WorldGeneratorDebugSettings.cs  # Gizmo toggles (see below)

Editor/World/
├── BiomeOverlayGizmoDrawer.cs      # NEW: Colored biome grid
└── BiomeGizmoDrawer.cs             # MOVED: Cell center labels

Editor/WorldGenerationWizard/ArtistMode/
├── ArtistModeWindow.cs
├── ArtistModeState.cs              # No ShowDebugVisualization
├── ArtistModeStyles.cs
├── Drawers/ (6 files)
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

## WorldGeneratorDebugSettings

All debug settings wired to their consumers:

| Field | Type | Used By | Purpose |
|-------|------|---------|----------|
| `drawBiomeGizmos` | bool | BiomeGizmoDrawer, BiomeOverlayGizmoDrawer | Master toggle |
| `gizmoAlpha` | float | All Gizmo Drawers | Gizmo transparency (0.1-1.0) |
| `biomeLabelHeight` | float | BiomeGizmoDrawer | Label offset above terrain |
| `drawCellCenters` | bool | BiomeGizmoDrawer | Show/hide disc markers |
| `drawRiverMarkers` | bool | RiverGizmoDrawer | Show/hide river discs |
| `logGeneration` | bool | GenerationPipeline | Master logging toggle |
| `logDetailedTimings` | bool | GenerationPipeline | Per-phase ms timing |
| `logWaterSync` | bool | TerrainSculptPhase | Water plane sync events |

**Removed**: `gizmoResolution` (was unused legacy from overlay quad approach)

---

## BiomeSO Decomposition ✅

### Pattern

`BiomeSO` now extends `ScriptableConfig<BiomeData>` with nested data classes:

```
BiomeSO : ScriptableConfig<BiomeData>
│
├── BiomeData (aggregator class)
│   ├── identity → BiomeIdentityData (type, debugColor, weight)
│   ├── waterBody → BiomeWaterBodyData (isWaterBody, floorDepth, etc)
│   ├── riverShore → BiomeRiverShoreData (style, gradient, width)
│   ├── height → BiomeHeightData (baseHeight, noise, curve)
│   ├── textures → BiomeTextureData (4 texture slots)
│   └── vegetation → BiomeVegetationConfig (categories, density)
│
└── scatterConfigs (List<BiomeScatterConfig>) — stays at SO level (runtime refs)
```

### Data Files

```
World/Biomes/Data/
├── BiomeData.cs            # Aggregator
├── BiomeIdentityData.cs    # Type, color, weight
├── BiomeWaterBodyData.cs   # Lake settings
├── BiomeRiverShoreData.cs  # River shore style
├── BiomeHeightData.cs      # Height & noise
└── BiomeTextureData.cs     # 4 texture slots + nested slot classes
```

### Convenience Accessors

BiomeSO provides backward-compatible accessors:
```csharp
public BiomeType type => Data.identity.type;
public float baseHeight => Data.height.baseHeight;
public BiomeVegetationConfig vegetationConfig => Data.vegetation;
// ... etc
```

---

## Vegetation System ✅ Category-Based

### Overview

Vegetation is now organized into **categories** by size (Small/Medium/Large), each with its own noise settings for natural distribution patterns.

### Architecture

```
BiomeVegetationConfig
├── globalDensity (0-3)
├── maxDensityPerCell (8-255)
└── categories[] → VegetationCategory[]
    ├── name ("Ground Cover", "Bushes", etc)
    ├── size (VegetationSize enum)
    ├── enabled
    ├── densityMultiplier (0-2)
    ├── noise → VegetationNoiseSettings
    │   ├── mode (Perlin, Voronoi, None)
    │   ├── scale (0.001-0.2)
    │   ├── threshold (0-1)
    │   ├── blend (0-0.5)
    │   ├── octaves (1-6)
    │   └── useStochastic
    ├── biomeEdgeFalloff (AnimationCurve)
    ├── slopeFalloff (AnimationCurve)
    ├── heightFalloff (AnimationCurve)
    └── layers[] → VegetationLayerConfig[]
        ├── prototype (VegetationPrototypeSO)
        ├── density (0-1)
        ├── weight (0.1-5)
        ├── useLayerNoise
        └── allowedTerrainLayers[]
```

### Size Categories & Default Noise

| Size | Default Scale | Threshold | Typical Content |
|------|--------------|-----------|------------------|
| Small | 0.05 | 0.3 | Ground cover, small grass |
| Medium | 0.025 | 0.45 | Bushes, flowers, tall grass |
| Large | 0.01 | 0.6 | Trees, large shrubs |

### VegetationPainter Flow

1. **Collect prototypes** from all biomes/categories
2. **Build masks** per biome+category using `MaskService`
3. **Paint cells** with terrain filtering (slope, height, texture layer)
4. **Apply density** from category → layer → noise modifiers

### Artist Mode UI

`VegetationSettingsDrawer` provides:
- Biome selector dropdown
- Per-biome global density slider
- Expandable category sections with:
  - Density multiplier
  - Noise scale, threshold, blend, octaves
  - Layer count info
- "Initialize Defaults" button (creates 3 default categories)
- "Clear Cache" button (clears mask cache for regeneration)

### Quick Actions

```csharp
// Initialize default categories
biome.vegetationConfig.InitializeDefaults();

// Clear mask cache for re-paint
VegetationPainter.ClearMaskCache();

// Repaint only vegetation (no terrain regen)
// Coming: VegetationPhase.RepaintOnly(context)
```

---

## Next Steps

1. ⏳ Test vegetation categories with different biomes
2. ⏳ Add real-time mask preview in Artist Mode
3. ⏳ "Repaint Vegetation Only" button (no full regen)
4. ⏳ Test river styles with different biomes
5. ⏳ Add water plane material (transparency, caustics)
6. ⏳ NavMesh baking test with slope limits
7. ⏳ Preset system for generation configs (REMINDER)
