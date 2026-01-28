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
| **Artist Mode Window** | ✅ Complete | `Editor/WorldGenerationWizard/ArtistModeWindow.cs` |
| **Debug Shaders** | ⏳ TODO | - |
| **Integration** | ✅ Complete | Button in GenerationConfigComposite |

### Completed Files

```
World/Generation/
├── Noise/
│   ├── INoiseSampler.cs              ✅
│   ├── NoiseSO.cs                    ✅
│   ├── Samplers/
│   │   ├── PerlinNoiseSO.cs          ✅
│   │   ├── SimplexNoiseSO.cs         ✅
│   │   ├── CellularNoiseSO.cs        ✅
│   │   ├── RidgedNoiseSO.cs          ✅
│   │   ├── BillowNoiseSO.cs          ✅
│   │   └── ValueNoiseSO.cs           ✅
│   ├── Modifiers/
│   │   ├── FBMNoiseSO.cs             ✅
│   │   ├── TurbulenceNoiseSO.cs      ✅
│   │   └── TerraceNoiseSO.cs         ✅
│   └── Combinators/
│       ├── NoiseBlendMode.cs         ✅
│       ├── CompositeNoiseSO.cs       ✅
│       └── NoiseMaskSO.cs            ✅
│
└── Pipeline/
    ├── IGenerationPhase.cs           ✅
    ├── GenerationPhaseBase.cs        ✅
    ├── GenerationContext.cs          ✅
    ├── GenerationPipeline.cs         ✅
    └── Phases/
        ├── BiomeLayoutPhase.cs       ✅
        ├── TerrainSculptPhase.cs     ✅
        ├── SplatmapPaintPhase.cs     ✅
        ├── VegetationPhase.cs        ✅
        └── ScatterPhase.cs           ✅

Utility/
└── ScriptableConfig.cs               ✅ Base class
```

---

## ScriptableConfig Pattern ✅

### Overview

Базовый класс для SO, оборачивающих конфигурационные данные. Разделяет данные (TData class) и контейнер (SO).

```csharp
// Base class
public abstract class ScriptableConfig<TData> : SerializedScriptableObject 
  where TData : class, new() {
  
  [HideLabel, InlineProperty]
  protected TData _data = new();
  
  public TData Data => _data;
}
```

### Refactored Configs

| ConfigSO | Data Class | Location |
|----------|------------|----------|
| `TreeFallConfigSO` | `TreeFallConfig` | Game/Trees/ |
| `WorldGridPresentationConfigSO` | `WorldGridPresentationConfig` | World/Grid/Presentation/ |
| `ColonyProgressionConfigSO` | `ColonyProgressionConfig` | Game/Progression/ |
| `WorldGeneratorConfigSO` | `WorldGeneratorConfig` | World/ |
| `BuildingManagerConfigSO` | `BuildingManagerConfig` | Building/Data/ |

### Usage Pattern

```csharp
// Define data class
[Serializable]
public class MyConfig {
  public float speed = 1f;
  public int count = 10;
}

// Create SO wrapper
[CreateAssetMenu(menuName = "Config/My Config")]
public class MyConfigSO : ScriptableConfig<MyConfig> {
  // Methods that use data go here
  public float GetAdjustedSpeed() => Data.speed * 1.5f;
}

// Access in code
var config = myConfigSO.Data;  // returns MyConfig
```

### Design Decision: Class vs Struct

**Chose class** because:
- Honest about reference semantics
- No false sense of "copy" when containing Lists
- Explicit Clone() when deep copy needed
- More flexible for complex configs

---

## Remaining Tasks

### 1. ArtistModeWindow.cs ✅

**Location**: `Editor/WorldGenerationWizard/ArtistModeWindow.cs`

**Features:**
- Per-phase Run/Rollback controls
- Status icons (○ Pending, ● Running, ◉ Completed, ✗ Failed)
- Seed control with randomize button
- Run All / Reset / Quick Generate actions
- Debug visualization toggle
- Opens from World Generation Wizard or menu `AA/Artist Mode Window`

### 2. Debug Shaders

```
Shaders/Debug/
├── BiomeDebug.shader                 ⏳
├── HeightGradient.shader             ⏳
└── DensityHeatmap.shader             ⏳
```

### 3. Integration

```
Editor/WorldGenerationWizard/
├── ArtistModeWindow.cs               ⏳ Next
└── PhaseProgressDrawer.cs            ⏳
```

---

## Overview

Система пошаговой генерации мира с возможностью остановки на каждой фазе для ручной калибровки. Включает богатую систему шумов с превью и комбинаторикой.

---

## Architecture

### Core Patterns

| Pattern | Usage |
|---------|-------|
| **Pipeline** | Sequential phase execution with pause points |
| **Strategy** | Interchangeable noise algorithms |
| **Composite** | Noise combinations and layering |
| **Observer** | Phase progress events for UI |
| **Memento** | Seed-based deterministic state (implicit) |

### Module Structure

```
World/
├── Generation/
│   ├── Pipeline/
│   │   ├── IGenerationPhase.cs         # Phase interface
│   │   ├── GenerationPhaseBase.cs      # Abstract base
│   │   ├── GenerationContext.cs        # Shared state
│   │   ├── GenerationPipeline.cs       # Orchestrator
│   │   └── Phases/
│   │       ├── BiomeLayoutPhase.cs     # Voronoi regions
│   │       ├── TerrainSculptPhase.cs   # Heightmap
│   │       ├── SplatmapPaintPhase.cs   # Terrain textures
│   │       ├── VegetationPhase.cs      # Grass, bushes
│   │       └── ScatterPhase.cs         # Trees, rocks, actors
│   │
│   └── Noise/
│       ├── INoiseSampler.cs            # Sample interface
│       ├── NoiseSO.cs                  # Abstract SO base
│       ├── Samplers/
│       │   ├── PerlinNoiseSO.cs
│       │   ├── SimplexNoiseSO.cs
│       │   ├── CellularNoiseSO.cs      # Worley/Voronoi
│       │   ├── RidgedNoiseSO.cs
│       │   ├── BillowNoiseSO.cs
│       │   └── ValueNoiseSO.cs
│       ├── Modifiers/
│       │   ├── FBMNoiseSO.cs           # Fractal layering
│       │   ├── TurbulenceNoiseSO.cs    # Domain warping
│       │   └── TerraceNoiseSO.cs       # Stepped output
│       └── Combinators/
│           ├── CompositeNoiseSO.cs     # Multi-noise blend
│           ├── NoiseBlendMode.cs       # Blend operations enum
│           └── NoiseMaskSO.cs          # Masked combination

Editor/
└── WorldGenerationWizard/
    ├── ArtistModeWindow.cs             # Dockable panel (TODO)
    └── PhaseProgressDrawer.cs          # Phase toggle UI (TODO)
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

### Combinators

| Blend Mode | Formula |
|------------|---------|
| Lerp | `lerp(a, b, t)` |
| Add | `a + b` |
| Multiply | `a * b` |
| Min | `min(a, b)` |
| Max | `max(a, b)` |
| Screen | `1 - (1-a)(1-b)` |
| Overlay | Photoshop-style |
| Mask | `lerp(a, b, mask)` |

---

## Generation Phases

### Phase 1: BiomeLayoutPhase
- Generates Voronoi diagram
- Assigns biomes to cells based on weights
- Output: `BiomeMap`

### Phase 2: TerrainSculptPhase
- Applies heightmap per biome
- Uses `biome.heightNoise` if configured
- Output: Terrain heightmap

### Phase 3: SplatmapPaintPhase
- Paints base texture per biome
- Uses `biome.GetBaseLayerIndex()`
- Output: Terrain splatmap

### Phase 4: VegetationPhase
- Applies terrain detail layers (grass)
- Uses `biome.vegetationConfig`
- Output: Detail layers

### Phase 5: ScatterPhase
- Spawns prefabs (trees, rocks)
- Uses `biome.scatterConfigs`
- Output: GameObjects under `[Generated_Scatters]`

---

## Artist Mode Window (TODO)

### UI Layout

```
┌─────────────────────────────────────┐
│ 🌍 World Generation      [≡] [×]    │
├─────────────────────────────────────┤
│ Seed: [1234567___] [🎲]             │
├─────────────────────────────────────┤
│ ○ Biome Layout          [▶][↺]     │
│ ○ Terrain Sculpt        [▶][↺]     │
│ ○ Splatmap Paint        [▶][↺]     │
│ ○ Vegetation            [▶][↺]     │
│ ○ Scatters              [▶][↺]     │
├─────────────────────────────────────┤
│ [▶▶ Run All]  [⟲ Reset]  [⚡Quick] │
└─────────────────────────────────────┘

○ = pending (gray)
◉ = completed (green)
● = current/running (blue pulse)
```

---

## Decisions Made

1. **Noise library**: ✅ Unity.Mathematics
2. **Async generation**: ✅ Async с прогрессом для длинных операций
3. **Preset system**: ⏸️ Заложить основу позже (НАПОМНИТЬ!)
4. **Undo support**: ✅ Только в пределах фазы
5. **ScriptableConfig TData**: ✅ Class (не struct) - честная семантика для reference types

---

## Next Steps

1. **ArtistModeWindow.cs** - Dockable EditorWindow
2. **Debug shaders** - BiomeDebug, HeightGradient
3. **Integration** - кнопка в GenerationConfigComposite
4. **Testing** - создать тестовые noise assets
